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
