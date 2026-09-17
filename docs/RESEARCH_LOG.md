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

**Test:** Deterministic codec test constructs the PM3 NG `CMD_BREAK_LOOP` frame and wraps it as BWM request command 5000. Expected bytes: `7CC788130A00504D3361008018013361D0A9`. The resulting frame decodes back to command 5000 with the identical PM3 payload.

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
