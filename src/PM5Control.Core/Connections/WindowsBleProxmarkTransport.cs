using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using PM5Control.Core.Protocols.Bwm;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Connections;

/// <summary>
/// Native Windows BLE transport for the Proxmark5 Battery Wireless Module.
/// The PM5 BWM exposes a serial-like GATT service used by the upstream
/// pm5_ble_bridge.py implementation: service AE86, data characteristic AE88.
/// This transport carries BWM frames; PM3/NG protocol bytes are payload data
/// of the verified BWM transparent-forward command.
/// </summary>
public sealed class WindowsBleProxmarkTransport : IProxmarkTransport, IProxmarkAbortTransport
{
    public static readonly Guid DefaultServiceUuid = new("0000ae86-0000-1000-8000-00805f9b34fb");
    public static readonly Guid DefaultCharacteristicUuid = new("0000ae88-0000-1000-8000-00805f9b34fb");

    private readonly ulong _bluetoothAddress;
    private readonly Guid _serviceUuid;
    private readonly Guid _characteristicUuid;
    private BluetoothLEDevice? _device;
    private GattCharacteristic? _characteristic;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public WindowsBleProxmarkTransport(
        ulong bluetoothAddress,
        Guid? serviceUuid = null,
        Guid? characteristicUuid = null)
    {
        if (bluetoothAddress == 0)
            throw new ArgumentOutOfRangeException(nameof(bluetoothAddress));

        _bluetoothAddress = bluetoothAddress;
        _serviceUuid = serviceUuid ?? DefaultServiceUuid;
        _characteristicUuid = characteristicUuid ?? DefaultCharacteristicUuid;
    }

    public string TransportName => $"Bluetooth LE {_device?.Name ?? "Proxmark5"}";
    public bool IsConnected => _device?.ConnectionStatus == BluetoothConnectionStatus.Connected && _characteristic is not null;
    public event Action<ReadOnlyMemory<byte>>? DataReceived;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsConnected)
            return;

        await DisconnectAsync().ConfigureAwait(false);

        _device = await BluetoothLEDevice.FromBluetoothAddressAsync(_bluetoothAddress);
        if (_device is null)
            throw new InvalidOperationException($"Unable to open BLE device 0x{_bluetoothAddress:X12}.");

        var servicesResult = await _device.GetGattServicesForUuidAsync(_serviceUuid, BluetoothCacheMode.Uncached).AsTask(cancellationToken).ConfigureAwait(false);
        if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            throw new InvalidOperationException($"PM5 BLE service {_serviceUuid} was not found ({servicesResult.Status}).");

        var service = servicesResult.Services[0];
        var characteristicsResult = await service.GetCharacteristicsForUuidAsync(_characteristicUuid, BluetoothCacheMode.Uncached).AsTask(cancellationToken).ConfigureAwait(false);
        if (characteristicsResult.Status != GattCommunicationStatus.Success || characteristicsResult.Characteristics.Count == 0)
            throw new InvalidOperationException($"PM5 BLE characteristic {_characteristicUuid} was not found ({characteristicsResult.Status}).");

        _characteristic = characteristicsResult.Characteristics[0];
        _characteristic.ValueChanged += OnValueChanged;

        var notify = await _characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask(cancellationToken).ConfigureAwait(false);
        if (notify != GattCommunicationStatus.Success)
        {
            _characteristic.ValueChanged -= OnValueChanged;
            _characteristic = null;
            throw new InvalidOperationException($"PM5 BLE notifications could not be enabled ({notify}).");
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var characteristic = _characteristic;
        _characteristic = null;
        if (characteristic is not null)
        {
            characteristic.ValueChanged -= OnValueChanged;
            try
            {
                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.None).AsTask(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Disconnect is best-effort; the OS will release the GATT session.
            }
        }

        _device?.Dispose();
        _device = null;
    }

    public async Task<byte[]> SendAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default)
    {
        await WriteRawAsync(request, cancellationToken).ConfigureAwait(false);
        return Array.Empty<byte>();
    }

    /// <summary>
    /// Abort a running PM3 operation through the verified BWM transparent-forward path.
    /// The BWM command is APP_CMD_SEND_FORWARD_DATA (5000); its payload is the raw
    /// PM3 NG CMD_BREAK_LOOP frame. Upstream BWM firmware forwards that payload to
    /// the PM5 over its UART and returns forwarded device data as a broadcast.
    ///
    /// This is deliberately not a guessed BWM-specific BREAK command: 5000 is the
    /// documented/source-verified transparent-forward command, while 0x0118 is the
    /// source-verified PM3 CMD_BREAK_LOOP command carried inside it.
    /// </summary>
    public Task AbortCurrentOperationAsync(CancellationToken cancellationToken = default)
    {
        var pm3BreakLoop = Pm3NgFrame.EncodeCommand(Pm3CommandCode.BreakLoop);
        var bwmRequest = BwmFrameCodec.EncodeRequest(
            (ushort)BwmCommandCode.SendForwardData,
            pm3BreakLoop);
        return WriteRawAsync(bwmRequest, cancellationToken);
    }

    private async Task WriteRawAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken)
    {
        var characteristic = _characteristic ?? throw new InvalidOperationException("BLE transport is not connected.");
        if (!IsConnected)
            throw new InvalidOperationException("BLE device is not connected.");

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Windows handles ATT MTU fragmentation for the GATT write.
            var buffer = request.ToArray().AsBuffer();
            var result = await characteristic.WriteValueWithResultAsync(
                buffer, GattWriteOption.WriteWithoutResponse).AsTask(cancellationToken).ConfigureAwait(false);
            if (result.Status != GattCommunicationStatus.Success)
                throw new IOException($"PM5 BLE write failed ({result.Status}).");
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        var bytes = new byte[args.CharacteristicValue.Length];
        using var reader = DataReader.FromBuffer(args.CharacteristicValue);
        reader.ReadBytes(bytes);
        if (bytes.Length != 0)
            DataReceived?.Invoke(bytes);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _writeGate.Dispose();
    }
}
