# Proxmark5 BWM / ESP32-C2 Protocol Notes

> Research snapshot: 2026-08-19
> Upstream: `RfidResearchGroup/Proxmark5_BWM_esp32`
> Upstream firmware documentation references commit `245002f` and was updated 2026-08-06.

## Why this document exists

Proxmark5 has a dedicated Battery Wireless Module (BWM) based on ESP32-C2/ESP8684. It is not merely a generic Bluetooth adapter. The current upstream BWM firmware exposes a structured binary UART command protocol and implements BLE, Wi-Fi scanning, Wi-Fi forwarding, TCP/UDP, MQTT, NVS configuration and OTA operations.

PM5 Control Center must treat BWM as a first-class subsystem and must not infer its state from the main PM5 firmware alone.

## Verified hardware information from upstream BWM repository

The upstream README identifies:

- ESP32-C2 / ESP8684
- Wi-Fi 4
- Bluetooth 5 LE
- BQ27427YZFR battery fuel-gauge IC
- AW32001ECSR charger/power-management IC
- BTB_10P interface carrying power path, UART and I2C
- 5-pin debug/programming header

The BWM repository describes the module as providing wireless connectivity and battery charging/power management.

## BWM architecture

The documented design is a bidirectional passthrough:

```text
Proxmark5 host UART
       <->
ESP32-C2 BWM
       +---- BLE SPP
       +---- Wi-Fi TCP server
       +---- Wi-Fi TCP client
       +---- Wi-Fi UDP server
       +---- Wi-Fi UDP client
       +---- Wi-Fi MQTT client
       +---- Wi-Fi scanner
       +---- OTA
```

This is highly relevant to the planned Windows/Android client because a remote application can ultimately communicate with the BWM network/BLE endpoint and have the BWM forward the PM5 host traffic.

## BWM UART packet format

The current BWM firmware uses a binary packet protocol:

```text
HDR1 | HDR2 | CMD/TYPE | PAYLOAD_LEN | PAYLOAD | CRC16
 1       1       2 LE        2 LE        N        2 LE
```

Packet types:

| Type | Value | Header | Direction |
|---|---:|---|---|
| HOST_CMD | 0 | `7C C7` | Host -> BWM |
| SLAVE_RESP | 1 | `2D 3D` | BWM -> host |
| SLAVE_BCAST | 2 | `D2 D3` | BWM -> host |

CRC:

- CRC16-CCITT
- polynomial `0x1021`
- initial value `0xFFFF`
- covers header through payload

Default BWM command UART configuration documented upstream:

- 460800 baud
- 8 data bits
- no parity
- 1 stop bit
- no flow control
- RX timeout 200 ms
- RX buffer 4096 bytes

The UART baud rate can be queried and changed dynamically by BWM commands. The client must not assume 460800 forever; it should detect/query the current rate where possible.

## Command groups

The upstream command definitions currently allocate ranges approximately as follows:

- `1000+` system/general
- `1800+` OTA/reboot
- `2000+` Wi-Fi control/configuration
- `2200+` TCP server
- `2300+` TCP client
- `2400+` UDP server
- `2500+` UDP client
- `2600+` MQTT client

The source explicitly warns developers not to reorder existing commands because host-side compatibility depends on stable command numbers.

## System inspection commands

The BWM exposes structured commands for at least:

- firmware version
- device model
- free system heap
- system timestamp
- application compile date/time
- base MAC address
- current UART baud rate
- maximum supported UART baud rate
- NVS statistics
- log forwarding state
- log level
- system-ready state

The documented BWM device model is `0xDA10`.

The Inspector should use these commands to populate the BWM section of the diagnostic report and should preserve the raw response bytes for troubleshooting.

## Wi-Fi modes

The firmware documents three modes:

```text
0 = Wi-Fi disabled
1 = Wi-Fi scanner
2 = Wi-Fi forwarding
```

When forwarding is enabled, the documented forwarding types are:

```text
TCP server
TCP client
UDP server
UDP client
MQTT client
```

Wi-Fi configuration includes country, TX power, inactive timeout, DHCP, protocol, MAC, IP/gateway/netmask, hostname, target SSID/BSSID/authentication, PMF, reconnect interval and SNTP settings.

## Wi-Fi scanner

The BWM has a genuine Wi-Fi scanner. Scan results are sent asynchronously to the host as a BWM broadcast with type `8088`.

The documented scan result contains:

- encryption/authentication type
- SSID
- RSSI
- BSSID/MAC
- channel
- pairwise cipher
- group cipher
- Wi-Fi protocol capability flags
- WPS support flag

Therefore a future PM5 Control Center screen can implement a real table such as:

```text
SSID | BSSID | Channel | RSSI | Security | Protocol | WPS
```

This is a BWM feature, not a generic PM5 RF feature. It must be clearly labeled as such.

## BLE

The BWM uses NimBLE and exposes a BLE SPP-style passthrough service. The API includes state, send/receive callbacks, device name/address, advertising manufacturer data, bonding settings, TX power and battery-level characteristic handling.

Important distinction:

The BWM documentation describes Bluetooth 5 LE. Do not describe it as classic Bluetooth SPP merely because the firmware calls its service `app_ble_spp`; verify the actual GATT service/characteristics before implementing an Android transport.

## Battery

The hardware contains a dedicated BQ27427 fuel-gauge IC and an AW32001 charger/power-management IC. The BLE layer also has a battery-level characteristic, with `255` representing unknown in the current implementation.

This means the final Inspector should distinguish:

```text
Battery hardware:      DETECTED/EXPECTED depending on evidence
BLE battery level:     REPORTED
Fuel-gauge telemetry:  DETECTED only if directly exposed/verified
Voltage/current/temp:  UNKNOWN unless a verified interface exposes them
```

Do not fabricate voltage/current/temperature values simply because a fuel-gauge IC exists.

## Flash / OTA

The production partition definition documented upstream is 4 MB with dual OTA application slots. A separate 2 MB development/no-OTA partition definition exists.

The BWM exposes OTA operations through commands corresponding to begin, write, end and reboot.

PM5 Control Center must treat BWM OTA as a potentially destructive operation. It must never perform it automatically. Before enabling an OTA button, the application should verify:

1. exact BWM model
2. current BWM firmware version/commit if available
3. flash/partition compatibility
4. target image identity
5. checksum/signature policy available for the image
6. backup/rollback capability

## Power-on / standalone implications

The BWM installation guide states that the PM5 can operate from the attached lithium battery and describes a power button shutdown procedure. This supports the project's planned standalone/remote-operation model.

However, the exact behavior of PM5 host firmware after the wireless link disappears must be tested on the physical device. The client must not assume that the main PM5 automatically enters a desired standalone mode simply because the BWM is disconnected.

## Security and remote access design

The BWM can expose TCP server/client and MQTT functionality. This creates a remote-control surface. PM5 Control Center should therefore separate:

- local USB access
- local BLE access
- local LAN access
- remote routed access

Remote routed access should not be exposed directly to the public Internet. A VPN/private overlay or equivalent authenticated network should be preferred.

The application should display a warning before enabling network forwarding and should make the listening address/port visible.

## Implementation consequence for PM5 Control Center

The architecture now needs a dedicated BWM adapter:

```text
IBwmProtocol
  +-- GetVersion
  +-- GetDeviceModel
  +-- GetSystemInfo
  +-- GetMac
  +-- GetReadyStatus
  +-- GetWifiStatus
  +-- GetWifiConfig
  +-- WifiScan
  +-- GetBleStatus
  +-- GetBleConfig
  +-- GetTcpStatus/Config
  +-- GetUdpStatus/Config
  +-- GetMqttStatus/Config
  +-- GetBatteryStatus (only where exposed)
  +-- GetStorageInfo
  +-- ExportRawDiagnostics
```

The adapter must preserve the exact upstream command number and protocol revision in its metadata. It should not hard-code assumptions without a compatibility entry.

## Research conclusion

The BWM repository confirms that the Proxmark5 wireless subsystem is substantially more capable than the legacy Proxmark3 Bluetooth add-on model. In particular, the current BWM firmware contains a real binary command protocol, Wi-Fi scanning, TCP/UDP/MQTT forwarding, BLE passthrough, configuration persistence and OTA support.

This is sufficient justification for making BWM a major part of PM5 Control Center rather than a future optional accessory.

## Sources checked

- `RfidResearchGroup/Proxmark5_BWM_esp32/README.md`
- `RfidResearchGroup/Proxmark5_BWM_esp32/DEV.md`
- `RfidResearchGroup/Proxmark5_BWM_esp32/main/app_com_defs.h`
- `RfidResearchGroup/Proxmark5_BWM_esp32/components/app_uart_cmd/app_cmd_uart.h`
- `RfidResearchGroup/Proxmark5_BWM_esp32/components/app_ble_spp/app_ble_spp.h`
- `RfidResearchGroup/Proxmark5_BWM_esp32/components/app_wifi_scanner/app_wifi_scanner.h`
- `RfidResearchGroup/Proxmark5_BWM_esp32/components/app_wifi_connect/app_wifi_connect.h`
- `RfidResearchGroup/Proxmark5_BWM_esp32/components/app_tcp_server/app_tcp_server.h`
- `RfidResearchGroup/Proxmark5_BWM_esp32/components/app_ota_ops/app_ota_ops.h`
