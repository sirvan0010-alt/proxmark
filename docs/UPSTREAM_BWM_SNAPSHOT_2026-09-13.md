# PM5 BWM/ESP32 Upstream Snapshot — 2026-09-13

## Purpose

This snapshot records the upstream state used by PM5 Control Center for the Proxmark5 BLE/WiFi module (BWM/ESP32). It is provenance, not hardware verification.

## Upstream repository

- Repository: `RfidResearchGroup/Proxmark5_BWM_esp32`
- Branch: `master`
- Latest inspected merge commit: `e0c4982800eb7d1ca16045631f3460f9f176427c`
- Parent feature commit: `847e1e51c0188a36c0bff73cc0884d9846f63f97`
- Latest relevant change inspected: build instructions for ESP32-C2/BWM firmware
- Previous relevant commit: `d04bda6919ed9810707d1fce58c9e20ca704c12e` (`INSTALL.md` updates)
- Research date: 2026-09-13

## DEV.md evidence

The upstream `DEV.md` identifies the module as ESP32-C2 / ESP8684, device model `0xDA10`, with 2.4 GHz Wi-Fi and Bluetooth 5.0 LE. It documents a default UART of 460800 baud and a binary command/response/broadcast protocol with CRC16-CCITT. fileciteturn25file0L2-L2

The documented architecture includes BLE SPP passthrough, Wi-Fi scanner, TCP/UDP/MQTT forwarding, SNTP and OTA functionality. These are firmware/source capabilities documented upstream; they are not claims that a physical PM5+BWM connected to this application has been verified.

## CI evidence

The upstream GitHub Actions workflow `ESP-IDF Build and Package` was inspected at commit `e0c4982800eb7d1ca16045631f3460f9f176427c`.

The latest inspected run completed successfully and produced the firmware package. The successful job included ESP-IDF setup, firmware build, firmware merge, package preparation and artifact upload.

CI success proves that the upstream firmware is currently buildable in the published workflow. It does not prove PM5↔BWM UART wiring, physical BWM communication, BLE/Wi-Fi operation, or compatibility with a particular physical PM5 revision.

## Protocol relationship to Control Center

The repository already records verified BWM protocol provenance in `docs/BWM_PROTOCOL.md`, tied to upstream commit `b918166128e05455c2dcb4e232216d453bbf29ee`. That document establishes source-level evidence for frame format, CRC scope, command provenance and read-only adapter requirements. fileciteturn29file0L2-L2

The new upstream snapshot must therefore be treated as a **new firmware/capability baseline**, not as a replacement for the protocol provenance record.

## Evidence classification

### PROTOCOL / SOURCE VERIFIED

- BWM binary framing documented upstream.
- CRC16-CCITT parameters and framing documented upstream.
- BWM command/broadcast model documented upstream.
- ESP32-C2 / ESP8684 platform documented upstream.
- Device model identifier `0xDA10` documented upstream.
- Default UART 460800 documented upstream.
- Firmware build reproducibility demonstrated by successful upstream CI run.

### NOT HARDWARE VERIFIED

- Physical PM5+BWM communication.
- Physical BWM model identification from the connected device.
- Actual BLE connection through a PM5/BWM unit.
- Actual Wi-Fi scan/forwarding through a PM5/BWM unit.
- Actual TCP/UDP/MQTT forwarding through a PM5/BWM unit.
- Actual OTA operation on the target module.
- Battery/charger telemetry exposed by the firmware on the target hardware.
- End-to-end PM5 ARM ↔ BWM ↔ wireless transport behaviour.

## Required implementation rule

Do not promote upstream source/CI evidence directly to `HARDWARE VERIFIED`.

The PM5 Control Center evidence model must continue to distinguish:

`PROTOCOL VERIFIED` → source/protocol basis

`CI VERIFIED` → upstream firmware builds successfully

`HARDWARE VERIFIED` → reproducible observation on the physical PM5+BWM device

## Next physical session

The first real PM5+BWM session should capture, without mutation:

1. PM5 connection and transport identity;
2. PM5 ARM/version response;
3. BWM presence/model/version response;
4. BWM UART communication;
5. BWM asynchronous broadcast behaviour;
6. BLE status;
7. Wi-Fi status;
8. capability responses;
9. raw request/response frames where safe;
10. exact firmware versions/commits where exposed.

Only the individual observations that are actually captured and reproducible should be upgraded to `HARDWARE VERIFIED`.
