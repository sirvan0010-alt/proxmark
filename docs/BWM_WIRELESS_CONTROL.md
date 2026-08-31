# PM5 BWM wireless control boundary

Checked 2026-08-31 against the current `RfidResearchGroup/Proxmark5_BWM_esp32` `master` sources and the current PM5 development notes.

## Hardware path

The official BWM board integrates an ESP32-C2/ESP8684, battery fuel gauge and charger. Its 10-pin BTB interface carries power, UART and I2C; the 5-pin header is the debug/firmware-flashing interface.

The BWM firmware command UART currently defaults to 460800 baud and uses its own framed command protocol with CRC16. The firmware exposes system, Wi-Fi, TCP/UDP, MQTT, BLE and forwarding commands.

The important limitation is on the PM5 host side: the upstream Proxmark5 development documentation currently states that the ARM↔BWM communication driver is still TODO and that operating PM5 via BWM is not supported at this time. Therefore the existence of a physical UART between PM5 and BWM does **not** mean a stock PM5 USB session can already forward arbitrary BWM commands to the ESP8684.

## What Control Center can safely represent now

### Wi-Fi

| Capability | Current BWM status | Control Center status |
|---|---|---|
| Station / STA | Exposed | Supported command catalog |
| Scan / scan results | Exposed | Supported command catalog |
| AP / SoftAP | Disabled in upstream build | Not supported by current BWM firmware |
| Promiscuous / sniffer | ESP32-C2 API capability | Not exposed by current BWM command protocol |
| Beacon TX | ESP-IDF raw-TX API capability | Not exposed by current BWM command protocol |
| Probe Request TX | ESP-IDF raw-TX API capability | Not exposed by current BWM command protocol |
| Probe Response TX | ESP-IDF raw-TX API capability | Not exposed by current BWM command protocol |
| Action Frame TX | ESP-IDF raw-TX API capability | Not exposed by current BWM command protocol |
| Non-QoS Data TX | ESP-IDF raw-TX API capability | Not exposed by current BWM command protocol |
| 5 GHz | Not a capability of ESP32-C2 | Not supported |
| Deauth TX | No supported public BWM/raw-TX path | Not supported / do not guess |
| Disassoc TX | No supported public BWM/raw-TX path | Not supported / do not guess |

### Bluetooth LE

The current BWM command interface exposes manufacturer advertising data, device name/address, bonding, battery level, TX power and BLE SPP control. It does not expose a general BLE scan or general GATT client/server command surface.

## Raw 802.11 TX

`esp_wifi_80211_tx()` is an ESP-IDF API capability, not a PC-side API. The current BWM command definition contains no raw-802.11-TX command. Therefore the Control Center cannot directly invoke that function through the stock BWM firmware today.

If raw TX is eventually required, the correct architecture is a **small, explicitly versioned BWM firmware bridge** added only after the PM5 ARM↔BWM transport is verified. It must not be implemented by silently flashing or replacing the user's BWM firmware.

## Command catalog

`src/PM5Control.Core/Bwm/BwmCommandCatalog.cs` mirrors the command numbering from upstream `main/app_com_defs.h`. The desktop application exposes the complete catalog for inspection, grouped into System, OTA, Wi-Fi, TCP server/client, UDP server/client, MQTT, Bluetooth LE and Forward.

The catalog is deliberately not a generic raw-command terminal. A command being listed means that it is defined/exposed by the upstream BWM firmware interface; it does not mean that the current PM5 USB transport can reach it.

## No firmware flashing

The new Control Center Wi-Fi/BWM cards are diagnostic and evidence-first. They do not flash the ESP8684, do not invoke OTA, and do not send guessed BWM commands.
