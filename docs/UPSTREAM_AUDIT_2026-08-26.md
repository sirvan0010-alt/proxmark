# Upstream audit — 2026-08-26

This audit records only upstream findings that materially affect PM5 Control Center. The user's `sirvan0010-alt/proxmark` repository is intentionally not treated as an upstream source.

## 1. RfidResearchGroup/proxmark3 — NielDK BLE transport

Checked 2026-08-26.

Relevant commits observed today:

- `46ab575247df0d4e5f1ccea6cfaabadc147d3d1f` — `Implement BLE support in uart_posix.c`
- `3156f0bfd7bd3e695e7eb660e6b3f5a544a5c5b9` — `Add ble_posix.c to CMakeLists.txt`
- `c5c53d444fc6c6baef4275f0483e1b327c66d3b5` — `Increase RX buffer size and improve comments`
- `e71f37ef32085e39c6713c464ec9e836ed5286ef` — `Remove WITH_BWM_FORWARD from PLATFORM_DEFS`

The upstream Linux transport adds a `ble:<MAC>` transport, performs ATT MTU exchange, discovers the PM5 BWM SPP characteristic, enables notifications, sends with ATT Write Command and receives Handle Value Notification payloads. It also keeps an RX leftover buffer because one PM3 NG frame can span several BLE notifications.

The PM5 BWM firmware source independently defines:

- SPP service UUID16 `0xAE86`
- SPP data characteristic UUID16 `0xAE88`
- standard Battery Service `0x180F`
- battery level characteristic `0x2A19`

Therefore the Windows Control Center now implements a native Windows GATT transport using the same service/characteristic identifiers and preserves the PM3 NG byte stream unchanged.

### What was copied/adapted

- ATT/GATT transport concept
- MTU/write-size aware chunking
- notification-based RX
- persistent RX reassembly
- command-ID response correlation
- read-only probe boundary

### What was deliberately not copied

- Linux raw L2CAP socket code
- BlueZ dependencies
- POSIX-only serial abstractions
- arbitrary CLI execution
- firmware flashing/reset operations

Windows uses the native GATT API instead.

## 2. RfidResearchGroup/Proxmark5_BWM_esp32

Checked 2026-08-26.

The repository README identifies the BWM as an ESP32-C2 based wireless/battery module with Wi-Fi 4 and Bluetooth 5 LE. Its BLE SPP component defines service `0xAE86`, characteristic `0xAE88`, and battery service `0x180F` / level `0x2A19`.

The repository also documents a development rule that Bluetooth command codes begin at 4000 and that existing command ordering must not be changed. This is relevant to the BWM command registry, but the Control Center does not invent or issue those commands yet.

## 3. RRG/Iceman main-tree changes

The current RRG tree also contains active PM3/AT32 maintenance and other protocol changes. These are useful reference material, but they are not automatically PM5 firmware changes.

In particular, PM5 boot/clock/flash changes must be treated as firmware evidence rather than copied into the Control Center. We therefore do not transplant low-level firmware code into the C# client merely because it appears in the PM3 tree.

## 4. FeliCa / unrelated changes

Commit `727a7c879f5bfaa59f41012c49699b73fe568c69` is a large FeliCa command-knowledge change. It is not part of the PM5 transport/diagnostic milestone and is intentionally not copied into the Control Center.

## 5. Control Center changes resulting from this audit

The repository now contains:

- `WindowsBleProxmarkTransport.cs` — Windows-native PM5 BWM BLE SPP transport.
- `BleControlForm.cs` — BLE device discovery, connection and read-only PM3-NG diagnostics.
- `BleUiModuleInitializer.cs` — exposes the BLE panel from PM5 Control Center without duplicating the main diagnostic UI.
- `IPm3ReadOnlyTransport.cs` — shared transport boundary for PM3-NG read-only exchanges.
- `docs/UPSTREAM_AUDIT_2026-08-26.md` — this evidence record.

The BLE panel supports only the existing safe probe:

- `CMD_VERSION` `0x0107`
- `CMD_CAPABILITIES` `0x0112`
- `CMD_STATUS` `0x0108`
- `CMD_PING` `0x0109`

No firmware write, reset, flash or arbitrary BWM command is exposed by this transport.

## 6. Verification boundary

The BLE implementation is **source/protocol informed but not hardware verified** until a physical PM5 with the BWM BLE firmware is connected and the following are captured:

1. Windows discovers the PM5 BWM.
2. GATT service `0xAE86` is visible.
3. characteristic `0xAE88` supports notify/write-without-response.
4. `CMD_VERSION` returns a valid PM3-NG response.
5. `CMD_CAPABILITIES` returns a valid response.
6. `CMD_STATUS` does not create a response storm.
7. fragmented notifications are reassembled correctly.

Until that test, the UI must continue to label BLE as transport capability rather than hardware-proven support.
