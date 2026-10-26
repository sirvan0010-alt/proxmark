# PM5 BWM Sync Mini-Plan — 2026-09-28

This mini-plan is deliberately outside the main PM5 Control Center roadmap. It tracks fast-moving upstream PM5/BWM changes without changing the main architecture.

## 1. Upstream baseline

- RfidResearchGroup/proxmark3: current master checked 2026-09-28.
- RfidResearchGroup/Proxmark5_BWM_esp32: current master remains 4818511a2b179c61f80f54b5f825428cba51deb8 (2026-09-14).
- Current BWM BLE baseline: notification/bulk-transfer retry pacing fix.
- Current PM5 upstream additions: Wi-Fi/TCP forwarding, BWM ESP32 OTA over the PM3 command link, BWM BLE/Wi-Fi management commands.

## 2. Implement now

### M1 — Native Wi-Fi/TCP transport
Status: IMPLEMENTED in this branch.

- Add WifiTcpTransport.
- Use the native PM3-NG stream directly over TCP.
- Default port 7777.
- Preserve fragmented TCP reads and PM3 response matching.
- Provide the same read-only probe interface as USB/BLE.
- Provide explicit abort through CMD_BREAK_LOOP.

### M2 — Generic command boundary
Status: IMPLEMENTED.

- Add IPm3CommandTransport.
- Keep IPm3ReadOnlyTransport as the safe diagnostic boundary.
- USB/serial, BLE and Wi-Fi transports can explicitly opt into control operations.
- Do not silently turn diagnostic code into a command terminal.

### M3 — BWM ESP32-C2 OTA
Status: IMPLEMENTED at protocol level; hardware verification pending.

- CMD_PM5_BWM_ESP_OTA = 0x017E.
- BEGIN: action + uint32 image size.
- WRITE: action + maximum 240 firmware bytes.
- END: finalize and upstream firmware performs the reboot.
- ABORT on transfer failure.
- VERSION available for post-update confirmation.
- Reject images without ESP magic 0xE9.
- Reject images whose extended-header chip ID is not ESP32-C2 0x000C.
- Never claim physical success until a real PM5+BWM device confirms it.

### M4 — Current BWM command/provenance synchronization
Status: IMPLEMENTED.

Keep the command catalogue and compatibility database tied to the latest verified upstream snapshot. New command IDs are never inferred from numbering gaps.

### M5 — Desktop wireless control surface
Status: IMPLEMENTED.

- Main Control Center now exposes a dedicated BWM/Wireless tab.
- Wi-Fi/TCP connection to the PM5 BWM can be selected and tested.
- Windows BLE discovery and native BLE SPP connection are exposed.
- BWM version/status diagnostic is available over the selected wireless transport.
- BWM ESP32-C2 OTA is available through the same transport with image validation and progress reporting.

### M6 — Automatic upstream continuation
Status: ENABLED.

- A weekly automation checks the relevant upstream repositories, compares changes against the Control Center, updates the mini-plan when a source-verifiable change is safe to implement, and reports hardware-verification blockers.

## 3. Do not mark as solved yet

- Realtime LF/COTAG streaming over BLE/Wi-Fi: separate upstream data path; bulk-flow fixes do not prove realtime streaming.
- Physical PM5+BWM Wi-Fi verification.
- Physical PM5+BWM BLE verification for every control operation.
- Charger configuration as automatically safe for arbitrary battery packs.
- CEP stability.
- Production/stable recommendation for PM5+BWM.

## 4. Verification gate

For each new upstream transport/BWM change:

1. Source verification.
2. Protocol fixture/test.
3. Simulator/fault test.
4. CI build/test.
5. Physical PM5+BWM test when hardware is available.
6. Only then promote the feature from SOURCE_VERIFIED to HARDWARE_VERIFIED.

## 5. Stop condition

This mini-plan does not replace the main roadmap. The currently implementable software delta is covered by Wi-Fi/TCP, generic command transport, BWM OTA, desktop wireless UI, evidence/provenance and protocol tests. Remaining items are physical verification and the separate realtime streaming path. Future upstream commits reopen only the affected item.


## Addendum — 2026-10-02

The consolidated authoritative plan is [MASTER_PLAN_2026-10-02.md](MASTER_PLAN_2026-10-02.md). Do not interpret the existing IMPLEMENTED labels as physical hardware verification.

New upstream delta to review:

- RRG `proxmark3` PR #3650 / merge commit `2630310336c28fd04b4b11aea8e77a7895a55de9`: BWM firmware flashing over Wi-Fi. Compare exact protocol and semantics with `BwmEspFirmwareUpdater`; distinguish ESP32-C2 BWM OTA from PM5 ARM/FPGA firmware flashing.
- Current changelog adds BWM BLE pairing/passkey and bond management, TX power, Wi-Fi modem power-save, ESP32 power-save, and optional idle auto-off controls. Refresh command/capability provenance and add read-only diagnostics where supported.
- CEP fixes `f1cb4952086861f2e27c89fddf0d274269cff9d6` and `e6d7cd1f9d330b930073f32cda06e308774cd36d` remain relevant to compatibility and regression tests.
- The upstream README still warns that PM5 firmware is unstable and says not to install the BWM addon board for now. Preserve an already-installed user-reported working module, but do not turn that report into a general stable-install recommendation.

Next mini-plan work: source-diff PR #3650; compare updater; refresh BWM capability model; add fault tests; then await physical read-only validation. No firmware write is authorized by this documentation update.

## Upstream delta — 2026-10-02

BWM ESP32 latest inspected commit is now `b450b1336dfe00fb507efb535ff3d8a1d9d036a9` (merge PR #7, mDNS support), not the older `4818511...` BLE bulk-transfer baseline. The newer commit adds `components/app_wifi_mdns`, optional Kconfig `CONFIG_PM5_MDNS_ENABLE` (default y), `<hostname>.local` announcements and `_proxmark5._tcp` DNS-SD service on the TCP server port. The PM3 host client adds an mDNS connection hint in `256f30f...`.

Caution: the current PM5 BWM usage guide still says there is no mDNS responder; it is stale relative to the current BWM repository source. Build-time config may disable the feature, so the user's installed image must be queried/observed before displaying it as available.

Next mini-plan:
1. Add DNS-SD discovery in Control Center with manual IP/port fallback.
2. Expose mDNS as `UNKNOWN` until the device/network actually advertises the service.
3. Add tests for service filtering, empty/invalid TXT records, duplicate results, expiry and unavailable mDNS.
4. Keep BWM OTA and main PM5 ARM/FPGA flashing completely separate.
5. Run CI and prepare the read-only hardware baseline; no flashing in this mini-plan.

## Important correction — PR #3650 (2026-10-02)

PR #3650 is **main PM5 firmware flashing over a BWM wireless link using a BWM-aware bootrom**, not ESP32-C2 BWM OTA. It adds a polled UART4/app_com bridge in the bootrom, reports `DEVICE_INFO_FLAG_UNDERSTANDS_BWM_STREAM`, and the host flasher supports `tcp:`, `udp:` and `bt:` while refusing writes if the bootrom lacks that flag. Upstream says a BWM-capable bootrom must first be flashed over USB. Our `WifiTcpTransport` is a PM3-NG runtime transport and is not yet a bootloader OLD-frame transport.

Keep these paths separate:
- PM5 ARM/FPGA flash: BWM-aware bootrom wireless stream (PR #3650).
- BWM ESP32-C2 OTA: `CMD_PM5_BWM_ESP_OTA` over the existing BWM app_com link.
- BWM recovery: physical 5-pin header + esptool.

The BWM ESP OTA image guard also checks app signature `0xABCD5432` at offset `0x20`, in addition to ESP magic `0xE9` and chip ID `0x000C`. It sends up to 240 bytes per chunk, paces writes and retries the entire transfer because resume is unsupported. Never treat a lost END acknowledgement as a definite failed flash; reconnect and check the BWM version before retrying.

## DNS-SD discovery implementation — 2026-10-02

Implemented Core class `Pm5MdnsDiscovery` with:
- PTR query for `_proxmark5._tcp.local`;
- IPv4 multicast group `224.0.0.251:5353`;
- compressed DNS-name parsing;
- PTR/SRV/TXT/A/AAAA records;
- returned hostname, port and discovered IP addresses.

Unit tests use a synthetic compressed mDNS response. CI and real PM5+BWM network verification remain pending. If discovery returns no result, keep the device state UNKNOWN and allow manual IP/port connection.


## Upstream sync — 2026-10-03

### Verified upstream delta

- `RfidResearchGroup/proxmark3` commit `133b981512f317c38b836f208e9c39afd75b5f28`: BWM TCP server default changes from `7777` to `18888`. Follow-up commits `f9bd1c30105688ad4ae41613d490110e419aa602` and `63063bd030ae0e9cd196068bdf884d4225577fb1` update implementation/docs.
- `RfidResearchGroup/Proxmark5_BWM_esp32` PR #8 merge `8153c26efee3ba2bb8dd6485223ac0742e4b165f` fixes hangs on Wi-Fi stop/restart by stopping mDNS when Wi-Fi modes are disabled. Relevant commits include `559244798ae5905a10b8ceba61ff8e5f4d23dfc2` and `a4c108536dc2d3396413ed9aadc4c4c247f53fe6`.
- Existing mDNS discovery implementation is already present in Core and Windows UI from PR #13; the remaining issue is lifecycle/reconnect behaviour and validation, not adding a second discovery implementation.

### Implemented in Control Center

- `WifiTcpTransport.DefaultPort` is now `18888`; constructor default uses the constant. Explicit user-supplied ports remain supported.
- BWM compatibility registry now records the new upstream BWM commit, mDNS stop-on-Wi-Fi-disable behaviour, and TCP port transition.

### Still required

- [x] Add a unit assertion for `WifiTcpTransport.DefaultPort == 18888` (commit 65d1942); CI is running.
- [ ] Add explicit custom-port override test.
- Add discovery/reconnect tests for mDNS service disappearance during Wi-Fi disable and reappearance after Wi-Fi restart; DNS-SD no-result remains UNKNOWN.
- Inspect current upstream BWM source before implementing any BLE pairing/power-save write commands; retain read-only-first policy and do not infer BWM UART command IDs from CLI syntax.
- Add a separate bootrom/OLD-frame transport only after source-level protocol audit; never reuse PM3-NG `WifiTcpTransport` for main ARM/FPGA flashing.
- Physical PM5+BWM verification remains pending. No firmware writes are part of this update.

Evidence: source/commit verified 2026-10-03; Control Center code change committed separately; physical device behaviour not verified.

## Upstream sync — 2026-10-19

### New upstream evidence since 2026-10-12

- `RfidResearchGroup/proxmark3` merge `f3c1195b19263dfd324a54b4764a15fac90baced` (2026-10-17) fixes PM5 RTC snapshot coherence in `GetTickCount()`. Reading SBS before TIME/DATE prevents a second rollover from making the tick count move backwards and causing false receive timeouts. This is firmware-side; no client protocol change is needed.
- `RfidResearchGroup/Proxmark5_BWM_esp32` merge `4889d2986f7ebba4bd071d2b9895aa5b779ae81c` (2026-10-18) adds dynamic frequency scaling, BLE modem sleep, light sleep, a persisted runtime power-save switch, Wi-Fi modem power-save selection, link-state broadcasts, a 12 KiB UART RX ring, and a 0x55 wake preamble. Companion PM5 host support was merged earlier in proxmark3 PR #3626. Do not equate these settings with the PM5 ARM idle-power command: they are distinct controls.
- BWM commit `23bfa7b21004f91dba64468d398971919fca8eda` (2026-10-18) fixes `*_set_rx_callback` teardown ordering; callbacks must be cleared before deinitializing the TCP/UDP forwarding component.
- `RfidResearchGroup/Proxmark5_FlipperZero_FAP` merges `61dd3fb955c218d12b29a56ed1eff7c7f7ee2e8f` and `eb8b803e281f3e11ba3ccb872906fcadc10fb819` (2026-10-18) add an HF 14a reader page and scheduler/CPU/ISR timing diagnostics. These are FAP UI changes, not new PM5 Control Center transport commands.
- FPGA open-source synthesis work merged in proxmark3 commit `23ce7229a7917a574cea4596e8fe98b9cdb2c4b7` (2026-10-18), but issue #3710 reports worse timing than the GOWIN IDE, lowering PWM frequency and antenna drive voltage. Do not recommend open-source-generated PM5 FPGA images for hardware use until timing is resolved.
- Issue #3648 was reopened on 2026-10-18 and updated on 2026-10-19: `bwm_cmd()` still discards valid non-matching frames while waiting for a BWM response. This can lose `DATA_FORWARD`, `LINK_STATE`, or response/flow-control frames when a wireless client is connected. Treat BWM control operations during an active wireless session as unreliable until upstream fixes the shared UART parser; do not attempt a client-side workaround that guesses at BWM framing.

### Control Center defects found in review

- `Pm5StatusDecoder.DecodeCepStatus()` currently passes bytes 0–18 to the battery decoder, but byte 0 is the CEP-active flag. The battery structure starts at byte 1; the correct slice is `payload.Slice(1, BatteryPayloadLength)`. Existing CEP test offsets also need to move by one byte and the test file needs explicit `System.Buffers.Binary` and `System.Text` imports.
- The wireless UI still defaults the TCP port to 7777 even though `WifiTcpTransport.DefaultPort` is 18888. The UI should use that constant.
- The desktop wireless UI does not yet expose the structured battery and CEP snapshots. Add read-only actions with capability/version gating; keep BWM mutation, power-save setters, and all flashing operations disabled.
- Compatibility metadata is stale: BWM UART RX buffer should be 12288 bytes, source commit should advance to `23bfa7b21004f91dba64468d398971919fca8eda`, and the new power-management/link-state behaviour and callback fix must be recorded.

### Status / verification boundary

The above are source-verified findings only. The BWM power-save changes are not physical-hardware verified by this project. The reopened #3648 forwarding/parser issue and #3710 FPGA timing issue are active upstream risks. No firmware update or device mutation is authorized by this sync.


## Retry upload — 2026-10-19

### Correction after retry
- The CEP decoder offset fix is present on branch `pm5-bwm-sync-2026-10-19` (commit `2b57ee791d7757c808486f4caf5c1a00fe93d41d`); it is not merged to main.
- The prior upload attempt did not complete all related fixes. Test fixture/import updates, GUI default-port correction, compatibility registry refresh and research-log update remain pending until each file is located and CI can be run.
- Expected GUI default must use `WifiTcpTransport.DefaultPort` (18888), not a duplicated literal 7777.
- CEP status decoder tests must cover the active flag at byte 0, battery payload starting at byte 1, firmware string starting at byte 20, and rejection of malformed payload lengths. Confirm exact upstream structure before changing test fixtures.
- Latest BWM power-save features from `RfidResearchGroup/Proxmark5_BWM_esp32` commit `4889d2986f7ebba4bd071d2b9895aa5b779ae81c` are source-reported only. The UART RX buffer is 12288 bytes; callback teardown fix is `23bfa7b21004f91dba64468d398971919fca8eda`.
- Do not mark CI, device behaviour, BLE/Wi-Fi behaviour, OTA, or battery telemetry as verified without the relevant evidence. No physical hardware tests were performed in this sync.


### Retry upload completion update — 2026-10-19

The following previously listed repository edits are now committed on this branch:
- CEP decoder offset: already present in commit `2b57ee791d7757c808486f4caf5c1a00fe93d41d`.
- Explicit `System.Buffers.Binary` and `System.Text` imports plus malformed CEP length test: commit `08932797366ea3e85b0c4cdc4ee71d22f0daebc8` (includes the import correction from `c3eee8796eb211ab22f880bee1b904fe5b0bccef`).
- GUI TCP port now uses `WifiTcpTransport.DefaultPort`: commit `6810da701958e39f0af88ce6c3a953013d3f5bee`.
- BWM compatibility metadata refreshed: commit `73f8b44645fe819da541fd16444894c9957788c7`.
- Research log updated: commit `15a690c3db4a2a00a6f5fd6f67e130612551df0a`.

Validation remains pending: GitHub reported no combined status checks for the latest commit at the time of this update. No physical PM5+BWM test was run. Do not mark CI or hardware behaviour as verified.


## 2026-10-26 — Weekly upstream sync and CI correction

### Upstream delta since 2026-10-19
- `RfidResearchGroup/Proxmark5_BWM_esp32`: no commits after `23bfa7b21004f91dba64468d398971919fca8eda` (2026-10-18). Power-save/DFS/light-sleep feature set remains at merge `4889d2986f7ebba4bd071d2b9895aa5b779ae81c`; callback teardown fix remains current. No new BWM firmware release/OTA change was found in the current repository history.
- `RfidResearchGroup/Proxmark5_FlipperZero_FAP`: no commits after `eb8b803e281f3e11ba3ccb872906fcadc10fb819` (2026-10-18). Scheduler/CPU diagnostic page and HF14a reader remain the latest relevant UI additions.
- `RfidResearchGroup/proxmark3`: general master activity through 2026-10-24 is mostly unrelated to PM5/BWM (EMV/Calypso handling and an AVX2 warning fix). New directly relevant open PR `#3728`, head `8cdce6ab7857972004496aa46d4af9c70a4a8d12`, proposes clearing stale SPI1 RX overrun data in `cep_spi_data_available()` and on every exit from `cep_spi_write_sync()`. Its description reports a real PM5 + Flipper FAP hardware trial and improved HF14a CEP reads. This is still an OPEN PR, not merged upstream.
- PR #3728 is firmware-side SPI1/CEP reliability work. No desktop-client protocol change is justified; keep it tracked and do not copy firmware internals into Control Center.

### Control Center CI failure found and correction
- Existing PR #15 CI run `39213150350` failed on both Ubuntu and Windows in `Pm5StatusDecoderTests`.
- Root causes were incorrect test fixtures, not a compiler failure: the battery fixture had 21 bytes despite the source-backed 19-byte `bwm_battery_info_t` layout; the CEP fixture wrote SoC/voltage one byte too early and did not set gauge-ok.
- Corrected fixtures in commit `6c30b0afce371f480ad0586afde9dfc18c81ebe6`: battery payload now matches the 19-byte packed upstream structure; CEP fields are written at overall offsets 3 (SoC) and 5 (voltage), with BWM-present and gauge-ok set at offsets 1 and 2.
- The corrected commit is source-verified against upstream `include/pm3_cmd.h`; the new CI run must pass before the PR can be marked ready. Do not describe CI as passing until GitHub reports a successful run.

### Current decision
- Safe change applied: test fixtures only. No protocol/production decoder semantics were loosened.
- Track upstream PR #3728 as `OPEN_SOURCE_REVIEW`, not merged/verified by our project.
- No hardware tests were performed. BLE/Wi-Fi/OTA, CEP operation, and battery telemetry remain not hardware-verified.
- Upstream README stability warning remains in force: PM5 firmware is actively developed and the BWM addon is not currently recommended for installation.
