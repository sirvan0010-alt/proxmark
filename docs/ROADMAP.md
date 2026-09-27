# PM5 Control Center Roadmap

## Planning principle

The project is evidence-first and capability-driven. Upstream source changes update our models and tests immediately, but source/CI evidence is never promoted to physical-hardware verification without an observation on the target PM5.

Implementation order:

Upstream evidence → Protocol model + parser → Unit/simulator tests → Real PM5 baseline → Transport integration → Inspector / GUI → Safe write operations → Firmware/BWM management → Advanced RFID workflows → Android / multi-platform

## M0 — Foundation

Status: complete

- [x] project purpose
- [x] AI continuation context
- [x] architecture
- [x] compatibility model
- [x] upstream research snapshots
- [x] staged test plan
- [x] AI progressive-engineering workflow
- [x] simulator contract and evidence rules

## M1 — PM5 protocol and capability foundation

Status: implemented; hardware verification pending

- [x] solution/project structure
- [x] diagnostic value model
- [x] transport abstraction
- [x] PM3 NG framing and response correlation
- [x] BWM packet codec
- [x] CRC implementation
- [x] compatibility database loader
- [x] diagnostic report/evidence model
- [x] JSON diagnostic exporter
- [x] BWM read-only adapter
- [x] cancellation/abort contract
- [x] PM5 NG payload limit 4064
- [x] CMD_CAPABILITIES decoding through upstream schema v13
- [x] explicit BWM capability state
- [x] explicit CEP capability state
- [x] source-backed CEP protocol model
- [x] unit tests for v13 capabilities and CEP framing

### M1.1 — upstream synchronization

- [x] record exact RRG/proxmark3 commits
- [x] record BWM repository baseline
- [x] record CEP protocol provenance
- [x] keep unknown future capability versions raw
- [ ] automatically flag upstream capability-schema changes in CI
- [ ] automatically refresh the upstream watch report

## M2 — Real PM5 baseline

This is the current critical blocker.

- [ ] connect the user's physical PM5 by USB
- [ ] capture USB VID/PID and Windows device/driver information
- [ ] identify exact PM5 hardware revision
- [ ] identify ARM firmware
- [ ] identify FPGA image/version
- [ ] query CMD_CAPABILITIES
- [ ] identify BWM presence/model/version if fitted
- [ ] identify battery/power telemetry
- [ ] verify BigBuf and max command data size
- [ ] capture raw read-only request/response frames
- [ ] verify timeout/reconnect behaviour
- [ ] verify device-side abort over USB
- [ ] establish a reproducible baseline report
- [ ] compare the observed baseline with the upstream compatibility registry

Rule: no firmware update is part of M2.

## M3 — Transport matrix

Goal: prove which logical operations work over which transport.

### USB

- [ ] PM3 NG request/response
- [ ] unsolicited frames
- [ ] timeout/reconnect
- [ ] cancellation and device abort

### BWM / BLE

- [ ] physical BWM detection
- [ ] BLE transport
- [ ] BLE bulk transfer
- [ ] BLE retry/flow-control diagnostics
- [ ] BWM broadcasts
- [ ] wireless abort
- [ ] BLE settings/status
- [ ] evidence for Wi-Fi forwarding

### Wi-Fi/TCP

- [ ] TCP transport
- [ ] reconnect
- [ ] bulk transfer
- [ ] timeout/abort
- [ ] capability-aware feature gating

### CEP / Flipper Zero

- [x] source-backed handshake model
- [x] length-prefixed NG frame model
- [ ] host/Flipper bridge integration
- [ ] real PM5↔Flipper handshake observation in this project
- [ ] real post-handshake NG command verification

CEP must remain separate from USB/BLE/Wi-Fi because it is a PM5↔Flipper transport, not a direct PC transport.

## M4 — PM5 Inspector and Windows GUI

- [x] Windows desktop shell
- [x] device dashboard foundation
- [x] read-only connection/selection UI
- [x] diagnostic log foundation
- [ ] live PM5 identity page
- [ ] capabilities page
- [ ] ARM/FPGA page
- [ ] BWM/ESP32 page
- [ ] transport matrix page
- [ ] power/battery page
- [ ] compatibility explanation page
- [ ] JSON/Markdown report export wired to live Inspector
- [ ] driver guidance page
- [ ] baseline/session history

The GUI must consume structured Core data; it must never parse terminal text.

## M5 — Safe firmware and BWM management

### Firmware

- [ ] verified backup mechanism
- [ ] firmware package metadata
- [ ] hardware/firmware compatibility gate
- [ ] explicit confirmation
- [ ] update progress
- [ ] post-update verification
- [ ] recovery workflow
- [ ] no silent overwrite of original backups

### BWM

- [ ] BWM firmware version/update metadata
- [ ] BWM upgrade workflow only after hardware verification
- [ ] BLE/Wi-Fi configuration
- [ ] power-save controls
- [ ] auto-off controls
- [ ] battery/charger diagnostics
- [ ] safe readback after every configuration write

No charger setting is applied automatically from an upstream recommendation.

## M6 — Simulator and fault model parity

The simulator must model transport and protocol behaviours already proven in source or hardware.

- [ ] capabilities v13 fixture
- [ ] BWM compiled/not-compiled variants
- [ ] CEP attached/not-attached variants
- [ ] unsolicited broadcasts
- [ ] fragmented frames
- [ ] delayed responses
- [ ] stale/late responses
- [ ] timeout
- [ ] disconnect/reconnect
- [ ] cancellation
- [ ] device-side abort result
- [ ] BWM flow-control stall/recovery

Simulator results remain SIMULATED, never HARDWARE_VERIFIED.

## M7 — Advanced PM5 client

Only after M2–M6 are stable:

- [ ] graphical access to supported RFID functions
- [ ] capability-aware command discovery
- [ ] long-running operation progress
- [ ] safe cancellation
- [ ] trace/download workflows
- [ ] structured RFID result model
- [ ] evidence-labelled card analysis
- [ ] antenna/RF diagnostics where upstream support is established
- [ ] advanced automation

## M8 — Android / multi-platform

- [ ] shared Core
- [ ] Android transport layer
- [ ] USB OTG
- [ ] BLE
- [ ] Wi-Fi/TCP
- [ ] CEP/Flipper integration where useful
- [ ] shared diagnostic reports
- [ ] remote diagnostics

## Release discipline

Every release must state tested hardware revision(s), tested ARM firmware, tested FPGA version, tested BWM firmware, transports tested, capabilities schema tested, CEP status if applicable, known limitations, upstream repository/branch/commit/date used for compatibility research, and whether each statement is source, CI, host or hardware verified.

A successful build is not hardware compatibility proof.
