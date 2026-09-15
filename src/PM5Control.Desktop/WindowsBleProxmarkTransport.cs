using System.Buffers.Binary;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Bwm;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

/// <summary>
/// Native Windows GATT transport for the PM5 BWM BLE SPP service.
/// Upstream evidence: RfidResearchGroup/Proxmark5_BWM_esp32 defines SPP
/// service 0xAE86 and data characteristic 0xAE88.
///
/// Wire model is deliberately explicit:
/// host -> BWM APP_CMD_SEND_FORWARD_DATA (5000) -> raw PM3 NG command;
/// BWM -> host APP_BROADCAST_DATA_FORWARD (8089) -> raw PM3 NG response bytes.
/// The BWM acknowledgement (5000) is not a PM3 response and is therefore not
/// fed into the PM3 parser.
/// </summary>
internal sealed class WindowsBleProxmarkTransport : IProxmarkTransport, IPm3ReadOnlyTransport, IProxmarkAbortTransport
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
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly object _rxLock = new();
    private readonly List<byte> _rxBuffer = new();
    private readonly BwmStreamParser _bwmParser = new();
    private bool _notificationsEnabled;

    public WindowsBleProxmarkTransport(ulong bluetoothAddress)
    {
        _bluetoothAddress = bluetoothAddress;
        _bwmParser.FrameReceived += OnBwmFrame;
    }

    public string TransportName => "Bluetooth LE / PM5 BWM SPP";
    public bool IsConnected => _device is not null && _characteristic is not null && _notificationsEnabled;
    public event Action<ReadOnlyMemory<byte>>? DataReceived;

    public static async Task<IReadOnlyList<BleDeviceCandidate>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());
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
                if (!name.Contains("Proxmark", StringComparison.OrdinalIgnoreCase) && !name.Contains("PM5", StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(new BleDeviceCandidate(name, device.BluetoothAddress, info.Id));
            }
            catch { }
        }
        return result.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;
        cancellationToken.ThrowIfCancellationRequested();
        _device = await BluetoothLEDevice.FromBluetoothAddressAsync(_bluetoothAddress);
        if (_device is null) throw new InvalidOperationException($"Windows could not open BLE device 0x{_bluetoothAddress:X12}.");

        var services = await _device.GetGattServicesForUuidAsync(SppServiceUuid, BluetoothCacheMode.Uncached);
        if (services.Status != GattCommunicationStatus.Success || services.Services.Count == 0)
            throw new InvalidOperationException($"PM5 BWM BLE SPP service 0x{SppServiceUuid16:X4} was not found; status={services.Status}.");

        GattCharacteristic? characteristic = null;
        foreach (var service in services.Services)
        {
            var chars = await service.GetCharacteristicsForUuidAsync(SppCharacteristicUuid, BluetoothCacheMode.Uncached);
            if (chars.Status == GattCommunicationStatus.Success && chars.Characteristics.Count > 0)
            {
                characteristic = chars.Characteristics[0];
                break;
            }
        }
        if (characteristic is null)
            throw new InvalidOperationException($"PM5 BWM BLE SPP characteristic 0x{SppCharacteristicUuid16:X4} was not found.");
        if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            throw new InvalidOperationException("PM5 BWM BLE SPP characteristic does not advertise notifications.");

        characteristic.ValueChanged += OnValueChanged;
        var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
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
        var bwmRequest = BwmFrameCodec.EncodeRequest((ushort)BwmCommandCode.SendForwardData, request);
        var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * TimeoutMs / 1000;
        var debugFrames = new List<Pm3NgResponse>();
        var unmatched = new List<Pm3NgResponse>();
        await WriteChunkedAsync(bwmRequest, cancellationToken).ConfigureAwait(false);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = RemainingMilliseconds(deadline);
            if (remaining <= 0) throw new TimeoutException($"PM5 BLE transaction timed out waiting for CMD 0x{command:X4}; debug={debugFrames.Count}; unmatched={unmatched.Count}; TX={Convert.ToHexString(bwmRequest)}");
            while (TryTakeFrame(out var response))
            {
                if (response is null) continue;
                if (Pm3CommandCode.IsDebugResponse(response.Command)) { debugFrames.Add(response); continue; }
                if (response.Command == command) return new Pm3NgExchange(request, response, debugFrames, unmatched);
                unmatched.Add(response);
                if (command == Pm3CommandCode.Status && response.Command == 0x0208) continue;
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
        if (request.Length < Pm3NgFrame.CommandHeaderSize + Pm3NgFrame.PostambleSize)
            throw new InvalidDataException("PM5 BLE transport received an undersized PM3 NG command frame.");
        var command = BinaryPrimitives.ReadUInt16LittleEndian(request.Span.Slice(6, 2));
        var exchange = await SendReadOnlyAsync(command, cancellationToken).ConfigureAwait(false);
        return exchange.Response.RawFrame;
    }

    /// <summary>
    /// Abort a running PM3 operation through the verified BWM transparent-forward path.
    /// BWM APP_CMD_SEND_FORWARD_DATA (5000) carries the raw PM3 NG CMD_BREAK_LOOP frame.
    /// Upstream BWM firmware forwards that payload to the PM5; forwarded device data
    /// returns as APP_BROADCAST_DATA_FORWARD (8089).
    /// </summary>
    public Task AbortCurrentOperationAsync(CancellationToken cancellationToken = default)
    {
        var pm3BreakLoop = Pm3NgFrame.EncodeCommand(Pm3CommandCode.BreakLoop);
        var bwmRequest = BwmFrameCodec.EncodeRequest((ushort)BwmCommandCode.SendForwardData, pm3BreakLoop);
        return WriteRawAsync(bwmRequest, cancellationToken);
    }

    private async Task WriteChunkedAsync(byte[] data, CancellationToken cancellationToken)
    {
        var characteristic = _characteristic ?? throw new InvalidOperationException("BLE characteristic is unavailable.");
        var chunkSize = characteristic.MaxWriteValueSize;
        if (chunkSize <= 0) chunkSize = 20;
        for (var offset = 0; offset < data.Length; offset += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(chunkSize, data.Length - offset);
            var status = await characteristic.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(data.AsMemory(offset, count).ToArray()), GattWriteOption.WriteWithoutResponse);
            if (status != GattCommunicationStatus.Success) throw new IOException($"BLE write failed at offset {offset}/{data.Length}; status={status}.");
        }
    }

    private async Task WriteRawAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var characteristic = _characteristic ?? throw new InvalidOperationException("BLE characteristic is unavailable.");
        if (!IsConnected) throw new InvalidOperationException("BLE device is not connected.");
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var status = await characteristic.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(data.ToArray()), GattWriteOption.WriteWithoutResponse);
            if (status != GattCommunicationStatus.Success) throw new IOException($"BLE write failed; status={status}.");
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out var data);
        if (data is null || data.Length == 0) return;
        _bwmParser.Append(data);
        DataReceived?.Invoke(data);
    }

    private void OnBwmFrame(BwmFrame frame)
    {
        if (frame.Kind != BwmFrameKind.Broadcast || frame.CommandId != (ushort)BwmBroadcastType.DataForward)
            return;

        lock (_rxLock) _rxBuffer.AddRange(frame.Payload);
        _rxSignal.Release();
    }

    private bool TryTakeFrame(out Pm3NgResponse? response)
    {
        response = null;
        lock (_rxLock)
        {
            if (_rxBuffer.Count < Pm3NgFrame.ResponseHeaderSize) return false;
            var header = _rxBuffer.Take(Pm3NgFrame.ResponseHeaderSize).ToArray();
            if (!Pm3NgFrame.TryGetResponseLength(header, out var totalLength))
            {
                _rxBuffer.RemoveAt(0);
                return _rxBuffer.Count >= Pm3NgFrame.ResponseHeaderSize && TryTakeFrame(out response);
            }
            if (_rxBuffer.Count < totalLength) return false;
            var frame = _rxBuffer.Take(totalLength).ToArray();
            _rxBuffer.RemoveRange(0, totalLength);
            return Pm3NgFrame.TryDecodeResponse(frame, out response);
        }
    }

    private static int RemainingMilliseconds(long deadline)
    {
        var ticks = deadline - Stopwatch.GetTimestamp();
        if (ticks <= 0) return 0;
        return (int)Math.Min(int.MaxValue, Math.Ceiling(ticks * 1000.0 / Stopwatch.Frequency));
    }

    private static Guid BluetoothUuid(ushort uuid16) => new($"0000{uuid16:X4}-0000-1000-8000-00805F9B34FB");

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
        _writeGate.Dispose();
    }
}

internal sealed record BleDeviceCandidate(string Name, ulong BluetoothAddress, string DeviceId)
{
    public override string ToString() => $"{Name} · {FormatAddress(BluetoothAddress)}";
    private static string FormatAddress(ulong value) => string.Join(":", Enumerable.Range(0, 6).Reverse().Select(i => ((value >> (i * 8)) & 0xFF).ToString("X2")));
}
