namespace PM5Control.Core.Bwm;

public enum BwmCapabilityStatus
{
    FirmwareExposed,
    HardwareApiOnly,
    NotSupportedByCurrentFirmware,
    NotSupportedByChip,
    Unknown
}

public sealed record BwmWirelessCapability(
    string Category,
    string Name,
    BwmCapabilityStatus Status,
    string Evidence,
    string Note);

public static class BwmWirelessCapabilityCatalog
{
    public static IReadOnlyList<BwmWirelessCapability> All { get; } =
    [
        new("Wi-Fi", "Station (STA)", BwmCapabilityStatus.FirmwareExposed, "Current BWM command set 2000+", "Connect, disconnect and configure a station through the existing BWM firmware."),
        new("Wi-Fi", "AP / SoftAP", BwmCapabilityStatus.NotSupportedByCurrentFirmware, "Current BWM sdkconfig.defaults", "ESP-IDF SoftAP support is explicitly disabled in the upstream BWM build (CONFIG_ESP_WIFI_SOFTAP_SUPPORT=n)."),
        new("Wi-Fi", "Scan / all-channel scan", BwmCapabilityStatus.FirmwareExposed, "BWM scan command set 2003-2006 and scan component", "Scan results are forwarded asynchronously by the BWM firmware."),
        new("Wi-Fi", "Promiscuous / sniffer", BwmCapabilityStatus.HardwareApiOnly, "ESP32-C2 Wi-Fi API capability", "The chip/API can provide promiscuous capture, but the current BWM command interface does not expose a sniffer command."),
        new("Wi-Fi", "Beacon TX", BwmCapabilityStatus.HardwareApiOnly, "ESP-IDF raw 802.11 TX API", "The current BWM command interface has no raw-802.11 TX command, so Control Center cannot invoke it without a firmware bridge."),
        new("Wi-Fi", "Probe Request TX", BwmCapabilityStatus.HardwareApiOnly, "ESP-IDF raw 802.11 TX API", "API-capable, but not exposed by the current BWM command protocol."),
        new("Wi-Fi", "Probe Response TX", BwmCapabilityStatus.HardwareApiOnly, "ESP-IDF raw 802.11 TX API", "API-capable, but not exposed by the current BWM command protocol."),
        new("Wi-Fi", "Action Frame TX", BwmCapabilityStatus.HardwareApiOnly, "ESP-IDF raw 802.11 TX API", "API-capable, but not exposed by the current BWM command protocol."),
        new("Wi-Fi", "Non-QoS Data TX", BwmCapabilityStatus.HardwareApiOnly, "ESP-IDF raw 802.11 TX API", "API-capable, but not exposed by the current BWM command protocol."),
        new("Wi-Fi", "5 GHz", BwmCapabilityStatus.NotSupportedByChip, "ESP32-C2/ESP8684 hardware", "ESP32-C2 is a 2.4 GHz Wi-Fi device."),
        new("Wi-Fi", "Deauthentication TX", BwmCapabilityStatus.NotSupportedByCurrentFirmware, "No BWM command; not a supported public raw-TX frame type", "Do not mark as hardware verified or add a guessed firmware probe."),
        new("Wi-Fi", "Disassociation TX", BwmCapabilityStatus.NotSupportedByCurrentFirmware, "No BWM command; not a supported public raw-TX frame type", "Do not mark as hardware verified or add a guessed firmware probe."),
        new("Bluetooth LE", "BLE SPP", BwmCapabilityStatus.FirmwareExposed, "BLE commands 4021-4023", "The BWM firmware exposes SPP status/start/stop and a BLE data API."),
        new("Bluetooth LE", "Advertising manufacturer data", BwmCapabilityStatus.FirmwareExposed, "BLE commands 4000-4001", "The current firmware exposes manufacturer advertising data configuration."),
        new("Bluetooth LE", "Device name/address", BwmCapabilityStatus.FirmwareExposed, "BLE commands 4002-4007", "Read/write device identity settings are exposed."),
        new("Bluetooth LE", "Bonding", BwmCapabilityStatus.FirmwareExposed, "BLE commands 4008-4015", "Bonding enable, key and bonded-device management are exposed."),
        new("Bluetooth LE", "TX power", BwmCapabilityStatus.FirmwareExposed, "BLE commands 4018-4019", "TX power read/write is exposed."),
        new("Bluetooth LE", "Generic BLE scan", BwmCapabilityStatus.HardwareApiOnly, "ESP32-C2 BLE capability", "No corresponding scan command is present in the current BWM command definition."),
        new("Bluetooth LE", "Generic GATT client/server control", BwmCapabilityStatus.HardwareApiOnly, "ESP32-C2 BLE capability", "The current BWM command interface exposes SPP, not a general GATT command surface."),
        new("Transport", "BWM command UART", BwmCapabilityStatus.FirmwareExposed, "BWM app_uart + BTB_10P hardware interface", "Upstream BWM firmware uses a command UART; default baud is 460800. Host-side PM5 ARM bridging is a separate issue."),
        new("Transport", "BWM over PM5 USB without PM5 firmware support", BwmCapabilityStatus.NotSupportedByCurrentFirmware, "Upstream Proxmark5 development documentation", "The ARM↔BWM communication driver is still documented as TODO; therefore PC→PM5 USB→BWM command control is not currently guaranteed by stock PM5 firmware."),
    ];
}
