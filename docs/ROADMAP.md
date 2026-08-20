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
