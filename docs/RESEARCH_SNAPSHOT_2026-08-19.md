# Upstream Research Snapshot — 2026-08-19

## Important discovery

The RfidResearchGroup organization currently has a dedicated public repository:

`RfidResearchGroup/Proxmark5_BWM_esp32`

It describes the Proxmark5 Battery Wireless Module as an ESP32-C2/ESP8684 BLE/Wi-Fi module with a battery fuel gauge and charger/power-management IC. The repository was created in May 2026 and was updated in August 2026, confirming that this is an actively evolving part of the PM5 software ecosystem.

## What this changes

Our original assumption that the wireless subsystem would be something we might need to infer from the old Proxmark3 Bluetooth add-on was too conservative. There is now a dedicated PM5 BWM firmware project with a documented binary protocol and a substantial host-control API.

Therefore:

**BWM support moves from a future research item to a first-class M1/M2 implementation target.**

## Verified capabilities documented upstream

- ESP32-C2 / ESP8684
- Wi-Fi 2.4 GHz 802.11b/g/n
- Bluetooth 5 LE
- BLE passthrough
- Wi-Fi scanner
- Wi-Fi TCP server/client
- Wi-Fi UDP server/client
- MQTT client
- SNTP
- persistent configuration in NVS
- binary UART command/response/broadcast protocol
- OTA update operations
- battery fuel gauge and charger/power management hardware

## Wi-Fi scanner

The scanner is not hypothetical. The firmware has a dedicated scanner component and broadcasts structured scan results. The result structure includes SSID, BSSID, RSSI, channel, authentication/encryption information, cipher information, protocol capability flags and WPS support.

This will become a future GUI page, but it is not the first feature we implement against the physical PM5. First we must validate the actual BWM firmware installed on the user's device.

## Remote-control topology

The documented BWM architecture supports this conceptual topology:

```text
Android / Windows
       |
       | BLE or Wi-Fi/TCP
       v
PM5 BWM (ESP32-C2)
       |
       | UART passthrough
       v
Proxmark5 host
```

For remote LAN access:

```text
Android
  |
 VPN / private overlay
  |
 Home LAN
  |
 PM5 BWM TCP endpoint
  |
 PM5 host
```

Do not expose the PM5 TCP endpoint directly to the public Internet.

## Battery

The BWM README identifies BQ27427YZFR as the fuel gauge and AW32001ECSR as the charger/power-management IC. The BLE software has a battery-level characteristic, but not every physical battery metric is necessarily exposed through the host command API. The Inspector must therefore report only verified telemetry and mark unsupported fields as UNKNOWN.

## Firmware updates

The BWM firmware contains OTA operations and the production partition definition uses dual OTA application slots. This makes automated firmware management possible in the future, but it also makes the backup/compatibility gate mandatory.

No firmware update is to be performed during the first hardware inspection.

## Current conclusion

The project should now prioritize:

1. BWM binary protocol implementation
2. PM5 host transport discovery
3. read-only device/BWM inspection
4. diagnostic report generation
5. physical PM5 validation
6. only then GUI/network/Android implementation

The repository is changing quickly. All future research must continue to record exact commit/date information.
