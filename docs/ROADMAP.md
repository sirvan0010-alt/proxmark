# Roadmap

## M0 — Foundation

Status: **complete in repository; verification remains evidence-based**

- [x] project purpose
- [x] AI continuation context
- [x] architecture
- [x] compatibility model
- [x] upstream research snapshots
- [x] staged test plan
- [x] AI progressive-engineering workflow
- [x] simulator contract and evidence rules

## M1 — PM5 Inspector Core

Goal: inspect a real device without CLI knowledge.

### Implemented foundation

- [x] solution/project structure
- [x] diagnostic value model
- [x] transport abstraction
- [x] stream/framing layer
- [x] BWM packet codec
- [x] CRC implementation
- [x] response correlation primitives
- [x] asynchronous event dispatcher
- [x] compatibility database loader
- [x] diagnostic report/evidence model
- [x] JSON diagnostic exporter
- [x] BWM read-only adapter
- [x] unit-test coverage for BWM framing/parser/adapter and new report/compatibility components

### Still blocked on real hardware / integration

- [ ] CI build + tests VERIFIED on GitHub
- [ ] USB transport
- [ ] PM5 identification adapter
- [ ] end-to-end Inspector orchestration
- [ ] report exporter wired to real Inspector data
- [ ] hardware-observed payload layouts

The code above is implementation progress, not proof of hardware compatibility. `PROTOCOL VERIFIED` remains distinct from `HARDWARE VERIFIED`.

## M2 — Real Hardware Baseline

- [ ] connect user's PM5
- [ ] capture VID/PID
- [ ] identify hardware revision
- [ ] identify ARM firmware
- [ ] identify FPGA
- [ ] identify BWM/ESP32
- [ ] identify memory
- [ ] identify battery/power telemetry
- [ ] record verified baseline
- [ ] compare with upstream snapshot

No firmware update is part of M2.

## M3 — Windows GUI

- [ ] Windows desktop shell
- [ ] device dashboard
- [ ] connection selector
- [ ] Inspector pages
- [ ] logs
- [ ] JSON/Markdown export
- [ ] backup page
- [ ] compatibility page

## M4 — BWM networking

- [ ] Wi-Fi status
- [ ] Wi-Fi scanner
- [ ] TCP server/client management
- [ ] UDP management
- [ ] BLE status
- [ ] BLE transport
- [ ] private-LAN remote control

## M5 — Safe firmware management

- [ ] verified backup mechanism
- [ ] firmware package metadata
- [ ] compatibility gate
- [ ] update confirmation
- [ ] post-update verification
- [ ] recovery documentation

## M6 — Profiles and automation

- [ ] saved device profiles
- [ ] standalone configuration
- [ ] workflow engine
- [ ] scheduled actions
- [ ] event triggers
- [ ] logs and replay where safe/applicable

## M7 — Android

- [ ] shared core
- [ ] Android transport layer
- [ ] USB OTG
- [ ] BLE
- [ ] Wi-Fi/TCP
- [ ] device dashboard
- [ ] remote diagnostics

## M8 — Advanced PM5 client

Only after the foundations are stable:

- [ ] graphical access to supported RFID functions
- [ ] command discovery
- [ ] capability-aware UI
- [ ] feature availability by firmware/hardware
- [ ] advanced automation

## Release discipline

Every release should state:

- tested hardware revision(s)
- tested ARM firmware
- tested FPGA version
- tested BWM firmware
- transports tested
- known limitations
- upstream commit/date used for compatibility research

A release is not considered hardware-compatible merely because it builds successfully.


## Consolidated plan (2026-10-02)

The authoritative cross-session plan is now [docs/MASTER_PLAN_2026-10-02.md](MASTER_PLAN_2026-10-02.md). It consolidates current implementation status, PR/branch reconciliation, upstream BWM/CEP work, physical hardware gates, transport testing, safe firmware management and AI-agent handoff. Keep this roadmap and the BWM mini-plan synchronized with the master plan. The roadmap's historical phase checkboxes must not be interpreted as proof of physical PM5 verification.

## Upstream integration update — 2026-10-02

- [x] Decode PM5 capabilities schema v13 (unit tests pending CI).
- [x] Represent BWM and CEP as independent compile-time capabilities.
- [x] Add source-backed CEP handshake/length-prefix model; no direct-PC transport claim.
- [x] Record current BWM ESP32 mDNS source support and optional DNS-SD discovery requirement.
- [ ] Implement and test DNS-SD discovery with manual IP/port fallback.
- [ ] Verify mDNS, BLE and Wi-Fi behaviour on the user's physical PM5/BWM.

See [UPSTREAM_UPDATE_2026-10-02.md](UPSTREAM_UPDATE_2026-10-02.md). Source-level support is not physical-device verification.

## Wireless firmware path clarification — 2026-10-02

PR #3650 adds main PM5 ARM/FPGA firmware flashing over a BWM wireless stream through a BWM-aware bootrom. This is separate from ESP32-C2 BWM OTA (`CMD_PM5_BWM_ESP_OTA`) and physical ESP32 recovery. The Control Center does not yet implement the legacy bootloader/OLD-frame wireless flash transport. Before implementing, require the bootrom capability flag `DEVICE_INFO_FLAG_UNDERSTANDS_BWM_STREAM`, exact image compatibility, trusted image checksums and a recovery plan.

## mDNS discovery implementation — 2026-10-02

- [x] Add DNS-SD PTR query for `_proxmark5._tcp.local`.
- [x] Add IPv4 mDNS multicast discovery and DNS compressed-name parser.
- [x] Parse PTR/SRV/TXT/A/AAAA records and preserve hostname/port/address.
- [x] Add unit fixtures for compressed records and malformed packets.
- [ ] CI pass for the discovery implementation.
- [ ] Verify discovery on the user's PM5/BWM network; retain manual IP/port fallback.
