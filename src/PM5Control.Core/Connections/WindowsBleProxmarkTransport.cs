using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace PM5Control.Core.Connections;

/// <summary>
/// Native Windows BLE transport for the Proxmark5 Battery Wireless Module.
/// The PM5 BWM exposes a serial-like GATT service used by the upstream
/// pm5_ble_bridge.py implementation: service AE86, data characteristic AE88.
/// This transport carries raw BWM frames; command policy remains above it.
/// </summary>
public sealed class WindowsBleProxmarkTransport : IProxmarkTransport
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
        var characteristic = _characteristic ?? throw new InvalidOperationException("BLE transport is not connected.");
        if (!IsConnected)
            throw new InvalidOperationException("BLE device is not connected.");

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // The BWM BLE SPP accepts a GATT write and handles the response via
            // notifications. Windows handles ATT MTU fragmentation for us.
            var buffer = request.ToArray().AsBuffer();
            var result = await characteristic.WriteValueWithResultAsync(
                buffer, GattWriteOption.WriteWithoutResponse).AsTask(cancellationToken).ConfigureAwait(false);
            if (result.Status != GattCommunicationStatus.Success)
                throw new IOException($"PM5 BLE write failed ({result.Status}).");

            // The protocol layer already owns frame correlation. Returning an
            // empty response makes this transport usable for streaming/event
            // consumers; request/response adapters should consume DataReceived.
            return Array.Empty<byte>();
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
