# Roadmap

## M0 — Foundation

Status: **in progress**

- project purpose
- AI continuation context
- architecture
- compatibility model
- upstream research snapshots
- staged test plan

## M1 — PM5 Inspector Core

Goal: inspect a real device without CLI knowledge.

- [ ] solution/project structure
- [ ] diagnostic value model
- [ ] transport abstraction
- [ ] stream/framing layer
- [ ] BWM packet codec
- [ ] CRC implementation
- [ ] response correlation
- [ ] asynchronous event dispatcher
- [ ] compatibility database loader
- [ ] diagnostic report schema
- [ ] USB transport
- [ ] PM5 identification adapter
- [ ] BWM read-only adapter
- [ ] report exporter

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
