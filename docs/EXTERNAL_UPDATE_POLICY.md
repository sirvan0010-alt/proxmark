# External Update and Integration Policy

Checked: 2026-08-31

## Tracked sources

The canonical source registry is `compatibility/external-sources.json`.

When an external update is requested, inspect all tracked repositories, maintainers and reference sites. Do not stop at RRG master. Compare each relevant change against the current project before implementing it.

## Current integration priorities

### PM5/BWM

- RfidResearchGroup/proxmark3: primary upstream/reference source.
- nieldk/proxmark3: PM5/BWM development and transport reference.
- iceman1001, doegox, dxl, nemanjan00: maintainer/developer activity to monitor.
- PM5 BWM charging safety: default charging target is treated as 4.1 V / 4095 mV at the current protocol resolution. This is a safety-oriented project default; it is not a firmware flash instruction.
- PM5 USB/UART timeout work: import only verified protocol/client-side improvements and keep firmware-only patches clearly separated.

### NFC

`nfc-tools/libfreefare` is a reference for NFC/MIFARE/DESFire/Ultralight/NTAG/FeliCa API concepts. It is not automatically compatible with PM5. The first integration layer is read-only identification and evidence-labelled protocol mapping.

### BLE

The SmartLockPicking BLE HackMe is treated as an authorized training/lab reference. NielDK's `pm5_ble_bridge.py` is treated as a transport reference. The application architecture should keep the host BLE stack separate from the PM5 byte-stream transport so Windows, Linux and macOS can use their native BLE facilities.

## Implementation gates

A change can be implemented directly when it is:

1. read-only or diagnostic;
2. relevant to PM5 Control Center;
3. supported by a verifiable source;
4. compatible with the repository architecture;
5. license-compatible if source code is copied;
6. covered by tests or an explicit simulator contract where hardware is required.

A change remains documentation-only when evidence is incomplete.

Firmware flashing, destructive tag writes, credential/key extraction, authentication attacks, and active testing of devices without authorization remain outside the automatic integration path.

## Evidence labels

`DETECTED` and `REPORTED` require device/protocol evidence. `EXPECTED` is compatibility knowledge. `HYPOTHESIS` is an engineering proposal. `SIMULATED` is test-only behavior. `UNKNOWN` is used whenever evidence is insufficient.

## Latest web audit notes

- Current RRG activity includes active PM5 work and ongoing CI changes; the PM5-specific installation documentation still warns that BWM support and flashing workflows have caveats. Do not turn those warnings into automatic flashing behavior.
- The current RRG workflow shows active maintenance by `iceman1001`, reinforcing the need to monitor upstream commits rather than relying on old snapshots.
- The external NFC source remains a reference layer; the project must not silently import its implementation or licensing assumptions.
