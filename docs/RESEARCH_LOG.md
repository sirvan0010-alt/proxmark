# Research Log

This is the chronological evidence log for the PM5 Control Center.

## Rules

- Never convert a hypothesis into a fact without evidence.
- Record the source and date for upstream observations.
- Record the exact PM5 hardware/firmware identity for hardware observations when available.
- Distinguish PM3-derived knowledge from PM5-verified knowledge.
- When two sources disagree, preserve both observations and record the mismatch.
- Record relevant upstream transport/BWM changes even when they do not yet change PM5 capability claims.

## Entry template

### YYYY-MM-DD — Short title

**Question:**

**Observation:**

**Evidence level:** UNKNOWN / HYPOTHESIS / DOCUMENTED / PROTOCOL_VERIFIED / HARDWARE_VERIFIED

**Source:**

**Upstream commit/tag:**

**Hardware identity:**

**Test:**

**Result:**

**Next action:**

## 2026-09-17 — Upstream CMD_CAPABILITIES schema correction

**Question:** Does the current upstream capabilities schema match the decoder previously implemented in the Control Center?

**Observation:** Current upstream `RfidResearchGroup/Proxmark3/include/pm3_cmd.h` defines `CAPABILITIES_VERSION 11`. The `capabilities_t` layout is append-only: the original 13-byte core is followed by `max_cmd_data_size` from v9 and `em_size`/`em_allocated` from v11. The fourth flag byte now also contains `is_pm5`, `is_pm5_std_ant`, FPGA-flash and I2C-EEPROM indicators. The previous Control Center decoder only accepted v6 and had incorrect flag positions for newer hardware fields.

**Evidence level:** DOCUMENTED / SOURCE_VERIFIED

**Source:** `RfidResearchGroup/Proxmark3`, `include/pm3_cmd.h`

**Upstream commit/tag:** Current upstream master inspected 2026-09-17; exact source revision should be recorded again when a PM5 hardware verification is performed.

**Hardware identity:** None for our project.

**Test:** Added a v11 fixture covering PM5/RDV4 flags, baud rate, BigBuf, `max_cmd_data_size`, emulator size and allocation state. Unknown/truncated schemas remain UNKNOWN.

**Result:** The decoder now accepts documented schema versions 6 through 11 and preserves the evidence boundary for versions outside that range. This is a protocol-model correction, not proof of any particular physical PM5 response.

**Next action:** Validate the actual PM5 `CMD_CAPABILITIES` response over USB and compare its raw payload with the v11 upstream layout before promoting any hardware-specific identity to DETECTED.

## 2026-09-17 — Upstream USB receive robustness fix

**Question:** Does the current upstream transport contain a receive-side fix relevant to PM5 Control Center reliability?

**Observation:** `RfidResearchGroup/Proxmark3` fixed USB receive stalling when a transfer ends with a zero-length OUT packet and fixed handling of a command already buffered behind another packet. The change applies to both AT91 and AT32 USB CDC paths and deliberately leaves frame parsing unchanged.

**Evidence level:** DOCUMENTED / SOURCE_VERIFIED

**Source:** `RfidResearchGroup/Proxmark3`, upstream PR #3628 / cherry-pick

**Upstream commit/tag:** `71b558d6be9eac062350f5533b1c39838dcef9a3` (2026-09-16)

**Hardware identity:** None for our project.

**Test:** Upstream reports the issue as an occasional lost packet after a burst of `CMD_HF_MIFARE_EML_MEMSET` chunks. The implementation explicitly checks already-buffered NG data, acknowledges terminating zero-length packets, and rearms reception.

**Result:** This is a transport-layer robustness change relevant to our USB session state machine. It does not establish new PM5-specific capability and does not change the PM3 NG frame format.

**Next action:** Add regression scenarios to the PM5 Control Center simulator/core for endpoint-boundary transfer + ZLP, multiple commands in one burst, and already-buffered command processing. Preserve transport/parser distinction in diagnostics.

## 2026-09-17 — BWM/ESP32 BLE bulk-transfer baseline unchanged

**Question:** Has the official BWM/ESP32 repository added another BLE transport change since the previous PM5 snapshot?

**Observation:** The latest located BWM/ESP32 change remains the BLE bulk-transfer drop fix. No newer BWM/ESP32 commit was located in the current monitoring pass.

**Evidence level:** DOCUMENTED / SOURCE_VERIFIED

**Source:** `RfidResearchGroup/Proxmark5_BWM_esp32`

**Upstream commit/tag:** `4818511a2b179c61f80f54b5f825428cba51deb8` (2026-09-14)

**Hardware identity:** None for our project.

**Test:** Commit history reviewed for current BWM/ESP32 activity; latest relevant change is the BLE bulk-transfer drop fix.

**Result:** The BLE bulk-transfer fix remains the current BWM transport baseline. It is not evidence that realtime LF streaming over BLE/Wi-Fi is fully supported.

**Next action:** Keep BLE/BWM burst-loss and backpressure tests in the simulator; monitor for new BWM/ESP32 commits affecting framing, buffers, notification scheduling, Wi-Fi/TCP forwarding, OTA or diagnostics.

## 2026-09-15 — PM5 BWM wireless abort path

**Question:** Can a running PM5 operation be cancelled over the BWM BLE/Wi-Fi path, and what must the Windows Control Center do to support that safely?

**Observation:** Upstream `RfidResearchGroup/Proxmark3` PR #3629 adds BWM polling to `data_available()` and `data_available_fast()` so `CMD_BREAK_LOOP` is observed on a PM5 built with `PLATFORM_EXTRAS=BWM`. The upstream PR reports verification on physical PM5+BWM hardware over BLE. The PR also identifies a separate realtime-sampling data path defect that remains unresolved.

**Evidence level:** PROTOCOL_VERIFIED / HARDWARE_VERIFIED (upstream author's hardware test; not our hardware)

**Source:** `RfidResearchGroup/Proxmark3`, PR #3629

**Upstream commit/tag:** implementation `5c86fb47f1619011f51724fbb0b17c6eb1c8066e`; merge `da3400ff04f9177e7ea806a12afc5b93c45a385e`

**Hardware identity:** Upstream test reports Proxmark5 with BWM fitted, BLE transport; exact physical serial/revision is not recorded in the PR.

**Test:** Start `hw ping`, run a long/looping LF command over BLE, send `CMD_BREAK_LOOP`, then verify that subsequent `hw ping` requests still receive replies.

**Result:** Upstream reports that before the firmware fix the wireless device stopped responding after the running command timed out; after the fix the device leaves the loop and remains responsive. This establishes the device-side abort contract, not end-to-end verification of this Control Center.

**Next action:** Keep `CMD_BREAK_LOOP` as an explicit non-read-only PM3 control primitive; implement the Windows transport's abort path only after the exact transparent BWM forwarding wire path is verified. Do not wrap a raw PM3 frame in a guessed BWM command.

## 2026-09-15 — Verified BWM transparent-forward wire path

**Question:** What exact BWM frame must the Windows BLE transport send to deliver PM3 `CMD_BREAK_LOOP` to the PM5?

**Observation:** The current official `RfidResearchGroup/Proxmark5_BWM_esp32` firmware defines `APP_CMD_SEND_FORWARD_DATA = 5000`. Its UART command handler passes the command payload to `tx_data_forward_from_uart()`, which forwards that payload to BLE and/or the configured Wi-Fi endpoint. Forwarded device data returns through `APP_BROADCAST_DATA_FORWARD = 8089`. The BWM UART frame format is the source-verified request/response/broadcast framing already implemented by `BwmFrameCodec`.

**Evidence level:** PROTOCOL_VERIFIED (source-level; not our physical hardware)

**Source:** `RfidResearchGroup/Proxmark5_BWM_esp32`, `main/app_com_defs.h`, `main/main.c`, `components/app_uart_cmd/app_cmd_uart.{c,h}`

**Upstream commit/tag:** `4818511a2b179c61f80f54b5f825428cba51deb8` (current upstream master, 2026-09-14)

**Hardware identity:** None for our project.

**Test:** Deterministic codec test constructs the PM3 NG `CMD_BREAK_LOOP` frame and wraps it as BWM request command 5000. Current little-endian PM3 framing produces BWM bytes `7CC788130A00504D3361008018016133E468`. The resulting frame decodes back to command 5000 with the identical PM3 payload.

**Result:** The previously unresolved BWM transparent-forward wire path is now source-verified. Windows BLE abort support can therefore use `BwmFrameCodec.EncodeRequest(5000, Pm3NgFrame.EncodeCommand(0x0118))` without inventing a BWM BREAK command. Physical end-to-end PM5+BWM BLE abort remains unverified by this project.

**Next action:** Run the full CI gates, then add hardware-lab verification when a PM5+BWM unit is available. Separately investigate the upstream unresolved realtime LF streaming path; do not conflate it with the abort fix.

## Current research topics

- Exact PM5 hardware revision and USB identifiers.
- Exact ARM firmware and FPGA versions.
- Exact ESP32/BWM firmware and exposed diagnostics.
- PM5-specific protocol additions versus PM3-compatible protocol.
- Available USB, Wi-Fi/TCP and BLE transports.
- BWM transparent-forwarding path for PM3 NG commands and `CMD_BREAK_LOOP` — source-verified; hardware end-to-end still open.
- Wireless cancellation, retry, backpressure and stale-response handling.
- Upstream realtime LF streaming over BWM/BLE/Wi-Fi — separate unresolved path.
- Power/battery telemetry exposed by the actual hardware.
- Verified firmware backup/extraction mechanisms.
- Compatibility differences between PM3-family devices and PM5.
- Upstream USB receive robustness and endpoint-boundary behavior.
- Continuous monitoring of RfidResearchGroup/Proxmark5_BWM_esp32 for BLE/Wi-Fi/BWM firmware changes.
- Current upstream `CMD_CAPABILITIES` schema v11 and PM5-specific capability bits.

## 2026-09-28 — PM5 BWM/Wi-Fi/OTA sync

**Question:** Which current upstream PM5/BWM changes are ready to adopt in the Control Center?

**Observation:** Upstream PM5 documentation currently defines native Wi-Fi STA + TCP forwarding on port 7777, BWM ESP32 OTA over the PM3 command link, BLE management, Wi-Fi power-save management and BWM bulk-flow pacing. The latest BWM/ESP32 repository commit remains `4818511a2b179c61f80f54b5f825428cba51deb8`. Current proxmark3 master additionally contains the PM5 Wi-Fi flashing path and the BWM ESP OTA command `CMD_PM5_BWM_ESP_OTA = 0x017E`.

**Evidence level:** SOURCE_VERIFIED

**Source:** RfidResearchGroup/proxmark3; RfidResearchGroup/Proxmark5_BWM_esp32; current PM5-BWM usage documentation.

**Upstream commit/tag:** proxmark3 master checked 2026-09-28; BWM/ESP32 `4818511a2b179c61f80f54b5f825428cba51deb8` (2026-09-14).

**Hardware identity:** None for our project.

**Test:** Added native Wi-Fi/TCP transport, explicit generic PM3 command transport boundary, source-verified BWM ESP32-C2 OTA protocol implementation and OTA image/chunking tests. Physical PM5+BWM end-to-end verification remains open.

**Result:** The Control Center now has protocol-level support for the current Wi-Fi/TCP and BWM ESP OTA delta without weakening the read-only diagnostic boundary.

**Next action:** CI, then physical PM5+BWM Wi-Fi/BLE/OTA verification. Keep realtime LF/COTAG streaming as a separate unresolved data-path item.

## 2026-10-02 — Integration CI audit

The first PR #13 CI pass exposed an inherited build failure in `Pm3SerialTransport`: it declared `IPm3CommandTransport` but had a duplicate `SendReadOnlyAsync` method and no payload-capable `SendCommandAsync`. The transport now has one read-only guard method and one generic payload-capable command method.

The next CI pass compiled Core/CLI/Simulator successfully but reported two test fixture issues:
- BWM OTA happy-path expected four commands for BEGIN + three WRITE chunks + END; correct count is five, with 240/240/20-byte chunks.
- A 4064-byte PM3 payload produces a 4076-byte response frame including 12 bytes of header/postamble, so it is two BWM/FPC 2048-byte chunks, not a third invalid slice.
- The BWM abort CRC vector was corrected to the little-endian byte order used by upstream `app_cmd_uart.c`.

These are repository/test corrections, not physical hardware findings. The subsequent CI run must verify them together with the newly added mDNS parser tests.

## 2026-10-02 — CI run 36937145061

The full Linux build/test matrix passed, including the new PM5 capabilities, CEP, OTA and mDNS parser tests. The Windows test matrix also passed. Windows desktop restore succeeded, but the single-file desktop publish failed on an inherited invalid C# character literal in MainForm2. It has since been corrected. An impossible image-length comparison in the OTA updater that generated a compiler warning was also removed.

The new mDNS button/result selector was added to active MainForm2 after this run, so the next full CI run must validate both that UI integration and the Windows publish step.

## 2026-10-02 — Windows BLE publish audit

The next Windows publish exposed inherited API assumptions in `WindowsBleProxmarkTransport`: `GattCharacteristic` does not expose `Dispose()` or `MaxWriteValueSize` in the Windows SDK target used by this project. Removed the invalid characteristic disposal calls and now use the guaranteed default ATT write payload of 20 bytes (MTU 23 minus 3-byte ATT header). Negotiated GATT MTU sizing can be added later through the supported session API. No BLE hardware transfer was performed. A fresh Windows desktop publish is required.

## 2026-10-02 — PR #13 merged

PR #13 merged to main as `69360c313cf5b68e85262dedc2cff38f90ada47f`. Final CI run `36937595521` passed both evidence gates, Ubuntu build/tests, Windows build/tests, Windows desktop restore and self-contained single-file publish. This includes the mDNS parser/UI, packed capabilities v12/v13 decoder, CEP model, BWM OTA guard changes and Windows BLE GATT API correction.

No PM5/BWM physical device was flashed or modified. The BWM OTA UI remains disabled. Next hardware phase is read-only baseline collection and bootloader capability detection.


## 2026-10-03 — BWM TCP port and mDNS lifecycle update

**Question:** What changed upstream after the 2026-10-02 mDNS integration?

**Observation:** RRG proxmark3 changed the BWM TCP server default from 7777 to 18888. Implementation commit 133b981512f317c38b836f208e9c39afd75b5f28; follow-ups f9bd1c30105688ad4ae41613d490110e419aa602 and 63063bd030ae0e9cd196068bdf884d4225577fb1. BWM ESP32 PR #8 merged as 8153c26efee3ba2bb8dd6485223ac0742e4b165f and stops mDNS when Wi-Fi modes are disabled, preventing mDNS lifecycle hangs on Wi-Fi stop/restart.

**Evidence level:** SOURCE_VERIFIED

**Source:** RfidResearchGroup/proxmark3; RfidResearchGroup/Proxmark5_BWM_esp32.

**Hardware identity:** None; no physical PM5/BWM session.

**Test:** WifiTcpTransport default updated to 18888; explicit custom ports remain supported. Fresh CI and hardware validation not yet run.

**Result:** Control Center no longer defaults to the obsolete 7777 port. mDNS disappearance while Wi-Fi is disabled is expected; discovery/reconnect must retry rather than classify the device as absent.

**Next action:** Add default/override tests and mDNS stop/restart tests; audit BLE management command definitions. No firmware writes.
