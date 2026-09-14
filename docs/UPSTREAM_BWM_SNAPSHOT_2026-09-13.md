# PM5 BWM/ESP32 Upstream Snapshot — 2026-09-14

## Purpose

This snapshot records the current upstream state used by PM5 Control Center for the Proxmark5 BLE/WiFi module (BWM/ESP32). It is provenance, not hardware verification.

## Upstream repository

- Repository: `RfidResearchGroup/Proxmark5_BWM_esp32`
- Branch: `master`
- Latest inspected commit: `4818511a2b179c61f80f54b5f825428cba51deb8`
- Commit: `Merge pull request #4 from nieldk/master — Fix BLE bulk-transfer drops`
- Previous inspected baseline: `e0c4982800eb7d1ca16045631f3460f9f176427c`
- Research date: 2026-09-14

## New upstream transport-reliability evidence

Commit `4818511a2b179c61f80f54b5f825428cba51deb8` changes the BWM BLE notification retry delay from 100 ms to 10 ms for both no-memory and send-failure retry paths in `components/app_ble_spp/app_ble_spp.c`.

The same commit retains `UART_RX_BUF_SIZE` at 4096 bytes and documents that buffer as absorbing device-to-host bursts while BLE drains them.

This is a source-level upstream transport reliability improvement. It does **not** by itself prove that a physical PM5+BWM unit connected to PM5 Control Center has reliable BLE bulk transfer.

## DEV.md evidence

The upstream `DEV.md` identifies the module as ESP32-C2 / ESP8684, device model `0xDA10`, with 2.4 GHz Wi-Fi and Bluetooth 5.0 LE. It documents a default UART of 460800 baud and a binary command/response/broadcast protocol with CRC16-CCITT.

The documented architecture includes BLE SPP passthrough, Wi-Fi scanner, TCP/UDP/MQTT forwarding, SNTP and OTA functionality. These are firmware/source capabilities documented upstream; they are not claims that a physical PM5+BWM connected to this application has been verified.

## CI evidence

The upstream repository has a published `ESP-IDF Build and Package` workflow. The previously inspected successful run at commit `e0c4982800eb7d1ca16045631f3460f9f176427c` proves the upstream firmware was buildable at that baseline.

The new commit `4818511a2b179c61f80f54b5f825428cba51deb8` must be treated as a newer source baseline until a successful CI run for that exact commit is independently recorded.

CI success proves build/package reproducibility only. It does not prove PM5↔BWM UART wiring, physical BLE/Wi-Fi operation, or compatibility with a particular physical PM5 revision.

## Protocol relationship to Control Center

The repository already records verified BWM protocol provenance in `docs/BWM_PROTOCOL.md`, tied to upstream commit `b918166128e05455c2dcb4e232216d453bbf29ee`. That document establishes source-level evidence for frame format, CRC scope, command provenance and read-only adapter requirements.

This snapshot is a **new firmware/capability baseline**, not a replacement for the protocol provenance record.

## Evidence classification

### PROTOCOL / SOURCE VERIFIED

- BWM binary framing documented upstream.
- CRC16-CCITT parameters and framing documented upstream.
- BWM command/broadcast model documented upstream.
- ESP32-C2 / ESP8684 platform documented upstream.
- Device model identifier `0xDA10` documented upstream.
- Default UART 460800 documented upstream.
- BLE retry behaviour change is present in upstream commit `4818511a2b179c61f80f54b5f825428cba51deb8`.
- UART receive buffer remains 4096 bytes in that commit.

### CI VERIFIED AT PREVIOUS BASELINE

- Firmware build/package workflow succeeded at `e0c4982800eb7d1ca16045631f3460f9f176427c`.

### NOT HARDWARE VERIFIED BY THIS SNAPSHOT

- Physical PM5+BWM communication.
- Physical BWM model identification from the connected device.
- Reliable BLE bulk transfer on the target unit after the new retry change.
- Actual Wi-Fi scan/forwarding through a PM5/BWM unit.
- Actual TCP/UDP/MQTT forwarding through a PM5/BWM unit.
- Actual OTA operation on the target module.
- Battery/charger telemetry exposed by the firmware on the target hardware.
- End-to-end PM5 ARM ↔ BWM ↔ wireless transport behaviour.

## Required implementation rule

Do not promote upstream source/CI evidence directly to `HARDWARE_VERIFIED`.

The PM5 Control Center evidence model must continue to distinguish:

`PROTOCOL VERIFIED` → source/protocol basis

`CI VERIFIED` → upstream firmware builds successfully at an exact commit

`HARDWARE VERIFIED` → reproducible observation on the physical PM5+BWM device

## Next physical session

The first real PM5+BWM session should capture, without mutation:

1. PM5 connection and transport identity;
2. PM5 ARM/version response;
3. BWM presence/model/version response;
4. BWM UART communication;
5. BWM asynchronous broadcast behaviour;
6. BLE status;
7. BLE bulk-transfer behaviour under representative PM5 traffic;
8. Wi-Fi status;
9. capability responses;
10. raw request/response frames where safe;
11. exact firmware versions/commits where exposed.

Only the individual observations that are actually captured and reproducible should be upgraded to `HARDWARE VERIFIED`.
