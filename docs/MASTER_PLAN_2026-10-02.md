# Proxmark5 Control Center — Master Plan
Date: 2026-10-02
Repository: `sirvan0010-alt/proxmark`
Target base: `main`
Scope: Consolidated handoff from the recent PM5 Control Center discussions, including the main roadmap, BWM mini-plan, upstream integration, hardware validation and AI continuation rules.

## 1. Project objective

Build a diagnostics-first Proxmark5 Control Center with a Windows GUI first, shared cross-platform Core, and later Ubuntu CLI / Android support. The user should be able to connect, diagnose, understand compatibility, export evidence and eventually manage supported features without having to type commands in CMD.

The project is not a generic PM3 client relabelled as PM5. PM3-derived functionality must be explicitly mapped to PM5 hardware, firmware and capabilities.

## 2. Non-negotiable safety and evidence rules

- Never flash automatically or silently.
- Do not recommend a firmware update merely because a newer commit exists.
- Do not remove a currently working BWM module solely because upstream has a general warning; preserve the user's present working configuration while avoiding a general stability recommendation.
- Upstream `RfidResearchGroup/proxmark3` currently warns that PM5 firmware is actively developed/unstable and says not to install the BWM addon board for now. Treat this as a release/stability warning, not proof that every already-installed board is non-functional.
- User-reported working/stable hardware is `REPORTED_BY_USER`, not independently hardware-verified by this repository.
- Keep these evidence states distinct: `SOURCE_VERIFIED`, `STATIC_ANALYSIS`, `UNIT_TESTED`, `CI_VERIFIED`, `PROTOCOL_VERIFIED`, `HARDWARE_OBSERVED`, `HARDWARE_VERIFIED`, `REPORTED_BY_USER`, `SIMULATED`, `UNKNOWN`.
- Do not promote a feature to hardware-verified without timestamped PM5/BWM evidence, exact hardware/firmware identity and repeatable test results.
- Preserve raw captures, transport, command, response, timing, retry count, client commit and confidence where practical. Never record Wi-Fi passwords, private keys or unnecessary secrets.
- Do not invent firmware dump, recovery, memory, battery or command payload behaviour.
- Main PM5 firmware flashing and ESP32-C2 BWM OTA are separate update paths and must never be conflated.
- BWM OTA image validation (ESP image magic 0xE9 and ESP32-C2 chip ID 0x000C) is necessary but does not prove that a physical update is safe or successful.

## 3. Current implementation baseline

The following is the repository-level baseline recovered from recent work. Status means implementation/source state, not automatic physical-device validation.

| Component | Current state | Remaining proof / work |
|---|---|---|
| PM3-NG framing/parser | Implemented and unit-tested | Capture exact PM5 transport behaviour |
| Read-only command boundary | Implemented | Verify selected commands on physical PM5 |
| BWM frame codec/CRC/parser | Implemented against upstream source | Physical PM5+BWM capture and command-by-command verification |
| Request/response correlation and events | Implemented in Core | Validate unsolicited events on hardware |
| USB/serial | Abstraction and desktop discovery present | Confirm actual driver/interface and end-to-end PM5 session |
| BLE | Transport/UI and BWM protocol work present | Verify discovery, connection, pairing, commands, reconnect and bulk transfers on device |
| Wi-Fi/TCP | Wi-Fi TCP transport and diagnostics present; upstream documents TCP server, default port 7777 | Verify the actual BWM endpoint, PM3-NG byte stream, reconnect and end-to-end commands |
| ESP32-C2 BWM OTA | Protocol updater and image checks implemented | Compare with current upstream Wi-Fi flashing path; test only after compatibility, backup and recovery review |
| CEP | Capabilities/model work exists on PR #10 branch | Reconcile PR with main; verify CEP behaviour and do not mistake it for PC transport |
| Windows UI | Desktop shell, inspector foundations and BWM/Wireless tab exist | Connect all views to verified data and clear evidence labels |
| Simulator/fault tests | Present | Expand timeout, disconnect, malformed frame, wrong command, broadcast and retry coverage |
| Diagnostic export | Data model/export foundations exist | Wire real Inspector data and include evidence provenance |
| Firmware manager | Not safe/complete for general PM5 use | Backup, package metadata, compatibility gate, explicit consent and recovery |
| Physical hardware baseline | Incomplete | Record exact device/firmware/driver/transport baseline |

## 4. Upstream baseline and changes to integrate

### 4.1 Repositories to monitor

- `RfidResearchGroup/proxmark3` — main PM5/PM3 firmware, protocol, capabilities, CEP, BWM command surface and host client.
- `RfidResearchGroup/Proxmark5_BWM_esp32` — ESP32-C2 firmware, BLE/Wi-Fi, battery/charger, OTA and BWM hardware integration.
- `nieldk/proxmark3` — PM5/BWM/BLE development reference.
- `nemanjan00/pm5-rdv4-antenna-adapter` — PM5/RDV4 hardware adapter reference.
- Additional repositories/accounts are added only when a concrete PM5-relevant change is found; unrelated offensive wireless projects stay outside normal Control Center transport and firmware scope.

### 4.2 Recorded upstream commits / PRs

- `RfidResearchGroup/proxmark3` PR #3650, merge commit `2630310336c28fd04b4b11aea8e77a7895a55de9`: **main PM5 firmware flashing over a BWM wireless stream via a BWM-aware bootrom**. It adds a BWM bootrom UART4 bridge, bootloader capability flag checks, and host flashing over wireless `tcp:`, `udp:` or `bt:` transports. It is not the ESP32-C2 BWM OTA path. A bootrom without `DEVICE_INFO_FLAG_UNDERSTANDS_BWM_STREAM` is rejected before any writes; upstream says flash a BWM-capable bootrom over USB once, then wireless flashing may be attempted.
- CEP timer/main-loop fix: `f1cb4952086861f2e27c89fddf0d274269cff9d6`.
- CEP SPI1 reply-corruption fix: `e6d7cd1f9d330b930073f32cda06e308774cd36d`.
- BWM ESP32 repository BLE bulk-transfer fix: `4818511a2b179c61f80f54b5f825428cba51deb8` (2026-09-14 baseline; re-check whether newer commits exist).
- Capabilities schema v13 / BWM and CEP flags were prepared in repository PR #10, branch `pm5-upstream-integration-2026-09-27`, head `c47142daf1afb3d8999ed74824c803cbde369d09`. PR #10 was still open and reported non-mergeable at the 2026-10-02 review. Do not assume these changes are in main; reconcile against current main and preserve unrelated commits.
- Wi-Fi TCP transport/BWM OTA work was merged to main through commit `cff1baab48fe86b86b7e10643f0dcaf220bb37c1` (PR #11 per commit message).
- Earlier PR #9 Wi-Fi transport branch is also open/stale relative to later merged work; do not merge it blindly.

### 4.3 Newly observed upstream command/features to track

Current upstream changelog and command reference include:
- `hw bwm ble`: persisted BLE on/off, pairing/passkey, bonded-device forget, TX power and status.
- `hw bwm autooff`: optional battery-idle power-off and unplug behaviour.
- `hw bwm wifipower`: Wi-Fi modem off and power-save mode.
- `hw bwm powersave`: ESP32 power-save settings.
- `hw bwm wifi`: Wi-Fi STA + TCP server management.
- `hw bwm upgrade`: ESP32-C2 BWM firmware update over an existing BWM app_com link; distinct from PM5 ARM/FPGA flashing.
- PR #3650 adds main PM5 firmware flashing over a BWM-aware bootrom's wireless byte stream; the bootrom must advertise `DEVICE_INFO_FLAG_UNDERSTANDS_BWM_STREAM` and wireless flashing must be refused otherwise.
- BWM master commit `b450b133...` adds optional mDNS (`<hostname>.local`, `_proxmark5._tcp`) with a Kconfig default of enabled; installed binary support remains unknown.
- Battery/charger telemetry and configurable charger operations require careful per-command verification; mutating charger operations must not be exposed as routine safe diagnostics.
- Bulk BLE/Wi-Fi transfer pacing does not prove real-time LF/COTAG streaming. Track that as a separate transport/data-path item.

For every upstream update, record repository, branch, commit SHA, date, changed paths, relevance, implementation decision, tests and hardware status.

## 5. Prioritized execution plan

### P0 — Preserve and identify the current physical setup

1. Do not flash PM5 or BWM during initial inspection.
2. Keep the user's already-installed module in place if the existing assembly remains functional; record that this is user-reported behaviour, not a blanket stability claim.
3. Capture PM5 hardware revision/identity, USB VID/PID, Windows driver/interface, ARM firmware, FPGA image/version, BWM/ESP32-C2 version and battery/charger identity where exposed.
4. Capture BLE and Wi-Fi identifiers/status without storing credentials.
5. Record a dated baseline diagnostic report and raw evidence.
6. Establish what backup/recovery is genuinely supported before any update is considered.

Exit condition: reproducible baseline report with unknowns explicitly marked.

### P1 — Reconcile upstream integration branches

1. Compare PR #10 (capabilities v13, CEP/BWM models, documentation/tests) against current main.
2. Reapply only missing, source-verified changes in a fresh branch; preserve current main and do not force-push.
3. Review PR #9 against merged Wi-Fi work in PR #11 / commit `cff1baab...`; close or supersede stale duplicate work only after confirming no unique changes are lost.
4. Confirm the current CI state on the resulting branch.
5. Update compatibility JSON and provenance together with code and tests.

Exit condition: one authoritative mainline implementation, no undocumented duplicate branch assumptions.

### P2 — Wireless firmware paths and BWM OTA audit

1. Keep three distinct paths: (a) PM5 ARM/FPGA flash through the BWM-aware bootrom over TCP/UDP/BLE, (b) BWM ESP32-C2 OTA through `CMD_PM5_BWM_ESP_OTA`, and (c) recovery through the physical 5-pin ESP header/esptool.
2. Inspect PR #3650's bootrom and host-flasher changes; identify the bootrom `DEVICE_INFO_FLAG_UNDERSTANDS_BWM_STREAM` gate and the USB-only prerequisite for installing a BWM-aware bootrom.
3. Compare BWM ESP OTA implementation with `BwmEspFirmwareUpdater`, including app signature `0xABCD5432`, 240-byte maximum chunks, write pacing, six whole-image attempts, lost END acknowledgement and post-update reset/version confirmation.
4. Add source-backed tests for image validation, begin/write/end errors, ambiguous finalization and retry policy.
5. Design a separate legacy bootloader/OLD-frame transport for wireless PM5 flashing; do not reuse the PM3-NG-only `WifiTcpTransport` without protocol proof.
6. Do not execute any firmware write on the user's hardware until exact hardware/bootrom identity, trusted image/checksum, compatibility, recovery path and explicit confirmation are established.

Exit condition: documented protocol equivalence or explicit incompatibility; no guessed OTA implementation.

### P3 — BWM diagnostics and management model

1. Add read-only capability/status coverage for BLE, Wi-Fi, TCP, battery, charger and BWM firmware.
2. Model BLE pairing state, bonded devices and TX power as source-versioned capabilities.
3. Model Wi-Fi radio/power-save and ESP32 power-save settings.
4. Model auto-off settings as read-only until a separate safe write review is completed.
5. Keep all state-changing commands behind a separate explicit command interface and human confirmation.
6. Mark fields unavailable on older firmware as UNKNOWN/UNSUPPORTED, not zero or false.

Exit condition: accurate BWM status report with per-field provenance and safe command separation.

### P4 — Transport and communication reliability

Build a repeatable test matrix for USB/serial, BLE and Wi-Fi/TCP:
- fragmented and coalesced frames;
- CRC errors and garbage bytes;
- response correlation and unsolicited broadcasts;
- wrong command IDs and unsupported responses;
- timeouts, cancellation and explicit abort;
- dropped connection during request and during bulk transfer;
- reconnect and stale-session handling;
- buffer limits/backpressure;
- latency/retry metrics.

Keep real-time LF/COTAG streaming as a separate research and implementation track. Do not infer it from successful bulk transfers.

Exit condition: deterministic unit/simulator suite plus a hardware test record per transport.

### P5 — PM5 Inspector and diagnostic reporting

1. Complete a single end-to-end read-only inspection workflow.
2. Show hardware identity, ARM, FPGA, BWM/ESP32, transport, power/battery and capabilities.
3. Explain PM3-vs-PM5 compatibility in human-readable terms.
4. Show DETECTED/REPORTED/EXPECTED/UNKNOWN and confidence/source for every important field.
5. Export JSON and human-readable Markdown reports with timestamps and software/upstream provenance.
6. Keep UI actions disabled until a compatible transport/session exists.

Exit condition: user can connect, diagnose and export without CMD.

### P6 — Safe firmware management (later; gated)

1. Define distinct package/update types for PM5 ARM, FPGA and ESP32-C2 BWM.
2. Verify package source, target, version, checksum and hardware compatibility.
3. Implement only documented backup methods; never invent firmware dumping.
4. Add recovery instructions and preflight checks.
5. Require explicit user confirmation for every write/flash operation.
6. Verify post-update versions and retain before/after reports.

Exit condition: no firmware operation can run from an unknown hardware identity or unverified package.

### P7 — GUI, automation and platform expansion

1. Finish Windows dashboard, device selector, logs, BWM/Wireless page, compatibility page and diagnostic export.
2. Add saved profiles and repeatable diagnostic workflows.
3. Keep automation read-only by default; state-changing actions require explicit opt-in.
4. Ubuntu CLI is a thin Core client.
5. Android is later and reuses shared protocol/Core code.
6. Advanced RFID command GUI comes only after the PM5-specific capability and safety foundation is stable.

## 6. Definition of done

A task is complete only when:
- source/protocol provenance is recorded;
- implementation and tests are present where applicable;
- CI result is recorded (or explicitly unavailable);
- docs, compatibility data and AI handoff are synchronized;
- hardware-only claims remain blocked until real evidence exists;
- changed files and commit/PR are recorded;
- next blocker and exact required evidence are stated.

## 7. AI agent operating instructions

For every continuation:
1. Read `AI_CONTEXT.md`, this master plan, `docs/ROADMAP.md`, `docs/AI_TASKS.md`, `docs/UPSTREAM_WATCHLIST.md` and the relevant mini-plan.
2. Inspect current main/branch/PR state before editing.
3. Complete the requested task, then inspect dependent code/tests/docs and take the next non-blocked step.
4. Never silently discard parallel-agent work or rewrite history.
5. Do not claim a GitHub update succeeded without a commit SHA and a fresh read.
6. Keep the main roadmap and BWM mini-plan synchronized with this document.
7. Report completed work, changed paths, tests/CI, commit/PR and remaining hardware blocker.
8. If a physical test is required, prepare exact steps and stop before unsafe firmware writes.

## 8. Immediate next actions

1. Reconcile PR #10 against current main.
2. Audit PR #3650's BWM-aware bootrom wireless flashing separately from ESP32-C2 BWM OTA.
3. Refresh the BWM upstream snapshot and command/capability registry, including BLE pairing, auto-off and power-save additions.
4. Complete the physical PM5/BWM identity and read-only diagnostic baseline.
5. Expand transport fault tests and end-to-end report export.

## Execution update — 2026-10-02

### Completed in current integration branch
- PR #12 (master plan consolidation) merged to main as `8241ad702d3fe5206920f3e8b3efb4583ba2d9fe`.
- Selectively ported PM5 capabilities schema v12/v13 decoder changes from the diverged PR #10 branch.
- Added BWM/CEP capability model and source-backed CEP handshake/length-prefix model.
- Added tests for v12/v13 flags, truncated and unknown payloads, independent capability gating and CEP frame prefix.
- Updated compatibility registries and upstream snapshot with the latest inspected BWM mDNS commit `b450b1336dfe00fb507efb535ff3d8a1d9d036a9` and Proxmark3 client commit `256f30f0fa7cb2fe84588f3d7ceb5eb3571a3363`.

### Newly discovered upstream delta
BWM firmware now has an optional mDNS responder (Kconfig default enabled) publishing `<hostname>.local` and `_proxmark5._tcp`. The PM5 usage guide still claims no mDNS; that guide is stale relative to the BWM firmware commit. Control Center should implement optional DNS-SD discovery but retain IP/port fallback. Do not claim the user's installed BWM image has mDNS until observed.

### Remaining in this integration
- Run GitHub Actions and resolve any compile/test failures.
- Reconcile docs and compatibility with current source.
- Open a replacement PR from current main; close stale PR #10 only after the replacement contains all intended changes.
- Keep physical hardware validation read-only; no firmware writes are part of this phase.

## mDNS implementation update — 2026-10-02

Core now contains `Pm5MdnsDiscovery`: PTR query for `_proxmark5._tcp.local`, IPv4 multicast discovery, compressed DNS name parsing, PTR/SRV/TXT/A/AAAA extraction, and result objects with hostname/port/addresses. Unit tests cover query shape, compressed PTR/SRV/A records and malformed/non-response packets. CI and physical PM5/BWM network verification remain pending. Manual IP/port entry remains the fallback; no mDNS response must never be interpreted as proof that the PM5 is absent.

## CI audit — 2026-10-02

CI caught and prompted fixes to an inherited serial command transport compile defect and two incorrect pre-existing test fixtures (BWM OTA chunk command count; PM3 response frame overhead at the BWM 2048-byte fragmentation boundary). The BWM abort CRC golden vector was aligned to upstream's low-byte-first CRC serialization. The follow-up CI run is still required to validate the current head, including mDNS tests.

### mDNS desktop integration

The Windows BWM/Wireless tab now exposes a **Discover (mDNS)** action. It searches for `_proxmark5._tcp.local`, lists discovered service instances and fills host/port only after the user selects a result. It never auto-connects. No results or multicast errors leave manual IP/port entry available and are not interpreted as proof that the device is absent. Windows desktop build/CI and physical network discovery verification remain pending.

## CI results — run 36937145061

- Upstream evidence gate: PASS.
- Evidence/claim audit: PASS.
- Ubuntu build + tests: PASS.
- Windows build + tests: PASS.
- Windows desktop restore: PASS.
- Windows single-file publish: FAILED due to an inherited invalid C# character literal in MainForm2; corrected afterward.
- mDNS parser unit tests were included and passed in this run.
- Desktop mDNS UI integration was added after this run and remains unverified by CI.

A new full run is required before merging PR #13.

### Firmware update controls remain gated

The Windows UI now keeps the BWM ESP32-C2 OTA button disabled. The protocol updater is source-audited and unit-tested, but the app does not yet enforce a trusted firmware package/checksum, exact device/firmware compatibility, a verified recovery path and post-update attestation. Main PM5 ARM/FPGA wireless flashing through the BWM-aware bootrom is a separate feature and is not yet implemented in the Control Center.

## Windows BLE API compatibility fix

CI run 36937392072 passed Ubuntu build/tests and Windows build/tests, but Windows single-file publish failed on unsupported `GattCharacteristic.Dispose()` and `MaxWriteValueSize` API assumptions. Removed characteristic disposal and changed write chunking to the guaranteed 20-byte default ATT payload. Negotiated MTU support remains a later optimisation. The latest head needs a fresh Windows publish before merge.

## Final integration status — 2026-10-02

### Merged to main
- PR #12: consolidated master plan and AI handoff — merged.
- PR #13: capabilities v13, CEP model, BWM OTA safety checks, mDNS Core discovery/UI, Windows transport compatibility fixes and regression tests — merged as `69360c313cf5b68e85262dedc2cff38f90ada47f`.
- Stale PR #10 was closed as superseded; its relevant changes were selectively ported onto current main.

### Final CI — run 36937595521
- Upstream Evidence Gate: PASS.
- Evidence / Claim Audit: PASS.
- Ubuntu Release build + tests: PASS.
- Windows Release build + tests: PASS.
- Windows desktop restore + self-contained single-file publish: PASS.
- Integration / Release Readiness Gate: PASS.

### Current boundaries
- Capabilities v12/v13, CEP framing model, BWM mDNS parser and Windows discovery UI are source/unit/CI verified.
- No physical PM5/BWM/Flipper hardware test was performed.
- BWM OTA remains disabled in the UI pending trusted package provenance, exact target/version compatibility, recovery procedure and post-update attestation.
- PR #3650's BWM-aware bootrom path for main PM5 ARM/FPGA wireless flashing is documented but not implemented in this Control Center. A separate legacy bootloader/OLD-frame transport and the bootrom capability flag check are required.
- mDNS no-result is UNKNOWN, not proof that the PM5 is absent; manual IP/port remains available.

### Next execution order
1. Prepare and run a read-only PM5/BWM hardware baseline: identity, bootrom/ARM/FPGA/BWM versions, USB/driver, BLE, Wi-Fi/TCP and mDNS.
2. Add a read-only bootloader Device Info parser and detect `DEVICE_INFO_FLAG_UNDERSTANDS_BWM_STREAM`.
3. Design the separate BWM-aware bootloader/OLD-frame wireless transport; do not reuse PM3-NG `WifiTcpTransport` without protocol proof.
4. Verify mDNS and BLE behaviour on the user's physical unit.
5. Only after recovery/package verification, revisit firmware update controls.

This is the current authoritative plan for future agents.
