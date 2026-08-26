using System.Collections.Concurrent;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

/// <summary>
/// Native Windows GATT transport for the PM5 BWM BLE SPP service.
/// Upstream evidence: RfidResearchGroup/Proxmark5_BWM_esp32 defines SPP
/// service 0xAE86 and data characteristic 0xAE88. The transport carries the
/// normal PM3 NG byte stream unchanged; BLE is only the transport layer.
/// </summary>
internal sealed class WindowsBleProxmarkTransport : IProxmarkTransport
{
    public const ushort SppServiceUuid16 = 0xAE86;
    public const ushort SppCharacteristicUuid16 = 0xAE88;
    public const ushort BatteryServiceUuid16 = 0x180F;
    public const ushort BatteryCharacteristicUuid16 = 0x2A19;
    private const int TimeoutMs = 3000;
    private const int MaxUnmatchedResponses = 32;

    private static readonly Guid SppServiceUuid = BluetoothUuid(SppServiceUuid16);
    private static readonly Guid SppCharacteristicUuid = BluetoothUuid(SppCharacteristicUuid16);

    private readonly ulong _bluetoothAddress;
    private BluetoothLEDevice? _device;
    private GattCharacteristic? _characteristic;
    private readonly SemaphoreSlim _rxSignal = new(0);
    private readonly object _rxLock = new();
    private readonly List<byte> _rxBuffer = new();
    private bool _notificationsEnabled;

    public WindowsBleProxmarkTransport(ulong bluetoothAddress)
    {
        _bluetoothAddress = bluetoothAddress;
    }

    public string TransportName => "Bluetooth LE / PM5 BWM SPP";
    public bool IsConnected => _device is not null && _characteristic is not null && _notificationsEnabled;
    public event Action<ReadOnlyMemory<byte>>? DataReceived;

    public static async Task<IReadOnlyList<BleDeviceCandidate>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var selector = BluetoothLEDevice.GetDeviceSelector();
        var devices = await DeviceInformation.FindAllAsync(selector);
        cancellationToken.ThrowIfCancellationRequested();

        var result = new List<BleDeviceCandidate>();
        foreach (var info in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var device = await BluetoothLEDevice.FromIdAsync(info.Id);
                if (device is null) continue;
                var name = string.IsNullOrWhiteSpace(device.Name) ? info.Name : device.Name;
                if (string.IsNullOrWhiteSpace(name)) continue;
                result.Add(new BleDeviceCandidate(name, device.BluetoothAddress, info.Id));
            }
            catch
            {
                // A stale Windows Bluetooth cache entry must not break discovery.
            }
        }

        return result
            .Where(x => x.Name.Contains("Proxmark", StringComparison.OrdinalIgnoreCase) ||
                        x.Name.Contains("PM5", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;
        cancellationToken.ThrowIfCancellationRequested();

        _device = await BluetoothLEDevice.FromBluetoothAddressAsync(_bluetoothAddress);
        if (_device is null)
            throw new InvalidOperationException($"Windows could not open BLE device 0x{_bluetoothAddress:X12}.");

        var servicesResult = await _device.GetGattServicesForUuidAsync(SppServiceUuid, BluetoothCacheMode.Uncached);
        if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            throw new InvalidOperationException($"PM5 BWM BLE SPP service 0x{SppServiceUuid16:X4} was not found; status={servicesResult.Status}.");

        GattCharacteristic? characteristic = null;
        foreach (var service in servicesResult.Services)
        {
            var chars = await service.GetCharacteristicsForUuidAsync(SppCharacteristicUuid, BluetoothCacheMode.Uncached);
            if (chars.Status == GattCommunicationStatus.Success && chars.Characteristics.Count > 0)
            {
                characteristic = chars.Characteristics[0];
                break;
            }
            service.Dispose();
        }

        if (characteristic is null)
            throw new InvalidOperationException($"PM5 BWM BLE SPP characteristic 0x{SppCharacteristicUuid16:X4} was not found.");

        if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            throw new InvalidOperationException("PM5 BWM BLE SPP characteristic does not advertise notifications.");

        characteristic.ValueChanged += OnValueChanged;
        var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify);
        if (status != GattCommunicationStatus.Success)
        {
            characteristic.ValueChanged -= OnValueChanged;
            characteristic.Dispose();
            throw new InvalidOperationException($"Could not enable PM5 BWM BLE notifications; status={status}.");
        }

        _characteristic = characteristic;
        _notificationsEnabled = true;
    }

    public async Task<Pm3NgExchange> SendReadOnlyAsync(ushort command, CancellationToken cancellationToken = default)
    {
        if (!IsConnected) throw new InvalidOperationException("PM5 BLE transport is not connected.");
        if (!Pm3CommandCode.IsSafeReadOnlyProbe(command))
            throw new InvalidOperationException($"Command 0x{command:X4} is outside the read-only BLE probe policy.");

        var request = Pm3NgFrame.EncodeCommand(command);
        var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * TimeoutMs / 1000;
        var debugFrames = new List<Pm3NgResponse>();
        var unmatched = new List<Pm3NgResponse>();

        await WriteChunkedAsync(request, cancellationToken).ConfigureAwait(false);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = RemainingMilliseconds(deadline);
            if (remaining <= 0)
                throw new TimeoutException($"PM5 BLE transaction timed out waiting for CMD 0x{command:X4}; debug={debugFrames.Count}; unmatched={unmatched.Count}; TX={Convert.ToHexString(request)}");

            while (TryTakeFrame(out var response))
            {
                if (response is null) continue;
                if (Pm3CommandCode.IsDebugResponse(response.Command))
                {
                    debugFrames.Add(response);
                    continue;
                }
                if (response.Command == command)
                    return new Pm3NgExchange(request, response, debugFrames, unmatched);

                unmatched.Add(response);
                if (command == Pm3CommandCode.Status && response.Command == 0x0208)
                    continue;
                if (unmatched.Count >= MaxUnmatchedResponses)
                    throw new TimeoutException($"PM5 BLE response storm while waiting for CMD 0x{command:X4}; last=0x{response.Command:X4}; RX={Convert.ToHexString(response.RawFrame)}");
            }

            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            waitCts.CancelAfter(remaining);
            await _rxSignal.WaitAsync(waitCts.Token).ConfigureAwait(false);
        }
    }

    public async Task<byte[]> SendAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default)
    {
        if (!Pm3NgFrame.TryGetResponseLength(request.Span, out _))
            throw new InvalidOperationException("Windows BLE transport expects a PM3 NG command frame; use SendReadOnlyAsync for the safe probe.");
        throw new NotSupportedException("Generic SendAsync is intentionally disabled. Use the read-only PM3 inspector boundary.");
    }

    private async Task WriteChunkedAsync(byte[] data, CancellationToken cancellationToken)
    {
        var characteristic = _characteristic ?? throw new InvalidOperationException("BLE characteristic is unavailable.");
        var chunkSize = characteristic.MaxWriteValueSize;
        if (chunkSize <= 0) chunkSize = 20;
        // The upstream PM5 BLE transport sends ATT Write Commands with payload
        // size MTU-3. Windows exposes the equivalent limit as MaxWriteValueSize.
        for (var offset = 0; offset < data.Length; offset += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(chunkSize, data.Length - offset);
            var chunk = data.AsMemory(offset, count).ToArray();
            var buffer = CryptographicBuffer.CreateFromByteArray(chunk);
            var status = await characteristic.WriteValueAsync(buffer, GattWriteOption.WriteWithoutResponse);
            if (status != GattCommunicationStatus.Success)
                throw new IOException($"BLE write failed at offset {offset}/{data.Length}; status={status}.");
        }
    }

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out var data);
        if (data is null || data.Length == 0) return;
        lock (_rxLock) _rxBuffer.AddRange(data);
        DataReceived?.Invoke(data);
        _rxSignal.Release();
    }

    private bool TryTakeFrame(out Pm3NgResponse? response)
    {
        response = null;
        lock (_rxLock)
        {
            if (_rxBuffer.Count < Pm3NgFrame.ResponseHeaderSize) return false;
            var header = CollectionsMarshal.AsSpan(_rxBuffer).Slice(0, Pm3NgFrame.ResponseHeaderSize);
            if (!Pm3NgFrame.TryGetResponseLength(header, out var totalLength))
            {
                // Preserve framing recovery rather than guessing a response.
                _rxBuffer.RemoveAt(0);
                return _rxBuffer.Count >= Pm3NgFrame.ResponseHeaderSize && TryTakeFrame(out response);
            }
            if (_rxBuffer.Count < totalLength) return false;
            var frame = _rxBuffer.Take(totalLength).ToArray();
            _rxBuffer.RemoveRange(0, totalLength);
            if (!Pm3NgFrame.TryDecodeResponse(frame, out response))
                return true;
            return true;
        }
    }

    private static int RemainingMilliseconds(long deadline)
    {
        var ticks = deadline - Stopwatch.GetTimestamp();
        if (ticks <= 0) return 0;
        return (int)Math.Min(int.MaxValue, Math.Ceiling(ticks * 1000.0 / Stopwatch.Frequency));
    }

    private static Guid BluetoothUuid(ushort uuid16) =>
        new($"0000{uuid16:X4}-0000-1000-8000-00805F9B34FB");

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var characteristic = _characteristic;
        if (characteristic is not null)
        {
            try { await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None); } catch { }
            characteristic.ValueChanged -= OnValueChanged;
            characteristic.Dispose();
        }
        _characteristic = null;
        _notificationsEnabled = false;
        _device?.Dispose();
        _device = null;
        lock (_rxLock) _rxBuffer.Clear();
        while (_rxSignal.CurrentCount > 0) _rxSignal.Wait(0);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _rxSignal.Dispose();
    }
}

internal sealed record BleDeviceCandidate(string Name, ulong BluetoothAddress, string DeviceId)
{
    public override string ToString() => $"{Name} · {FormatAddress(BluetoothAddress)}";
    private static string FormatAddress(ulong value) => string.Join(":", Enumerable.Range(0, 6).Reverse().Select(i => ((value >> (i * 8)) & 0xFF).ToString("X2")));
}
