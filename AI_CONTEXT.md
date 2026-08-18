# AI_CONTEXT.md — READ THIS FIRST

## Purpose

This file is the handoff document for future AI coding assistants working on **Proxmark5 Control Center**.

The project is intended to become an independent, diagnostics-first Proxmark5 client. It should make the physical device understandable and controllable without requiring the human user to know CLI commands.

## Current project stage

**M0 — Documentation and architecture.**

The project repository has just been initialized. No real Proxmark5 hardware state has been verified in this repository yet.

Do not claim that any PM5-specific function is working until it has been verified against a real device or a reliable upstream source.

## Immediate next task

When the physical Proxmark5 is available:

1. Connect by USB.
2. Inspect Windows device/driver information.
3. Do not install or replace a driver blindly.
4. Use Zadig only if the verified transport/client requirements actually require it.
5. Identify hardware and USB information.
6. Identify ARM firmware, FPGA and ESP32/BWM versions.
7. Identify available diagnostic/capability interfaces.
8. Determine what power/battery telemetry is actually exposed.
9. Preserve the original device state before firmware changes.
10. Record exact versions, build dates and commits where available.

Do not flash anything during the first inspection.

## Critical assumptions to avoid

- `proxmark3` repository name does not mean every component is PM3-only.
- Conversely, PM5 hardware does not mean every PM3 function is automatically compatible.
- Do not infer PM5 support solely from a source file existing in the RRG repository.
- Do not infer a physical memory size solely from a firmware constant.
- Do not report an expected value as a detected value.
- Do not guess battery percentage when only voltage is available.
- Do not assume Wi-Fi/BLE/TCP/BWM functionality is enabled merely because related code exists upstream.
- Do not treat the latest upstream branch as a stable API.

## Diagnostic truth model

Every important hardware/firmware fact should have a source state:

- `DETECTED` — verified by a direct, reliable mechanism.
- `REPORTED` — reported by the connected firmware/device.
- `EXPECTED` — derived from a compatibility definition.
- `UNKNOWN` — not safely determinable.

If multiple sources disagree, show the disagreement and reduce confidence.

## Confidence model

Use explicit confidence for important conclusions:

- `HIGH` — multiple reliable sources agree or a direct verification exists.
- `MEDIUM` — a reliable report exists but direct verification is unavailable.
- `LOW` — inferred from compatibility data or indirect evidence.
- `UNKNOWN` — insufficient evidence.

Never hide a mismatch to make the UI look cleaner.

## Architecture rules

Keep these layers separate:

```text
UI
 ↓
Application / ViewModel
 ↓
Device Inspector + Compatibility Engine
 ↓
Protocol Abstraction
 ├─ legacy PM3-compatible mechanisms
 ├─ PM5-specific mechanisms
 └─ ESP32/BWM API
 ↓
Transport
 ├─ USB
 ├─ BLE
 └─ Wi-Fi/TCP (when supported)
 ↓
Proxmark5
```

The diagnostic and protocol layers must not depend on a Windows-only UI implementation. This is required for the future Android client.

## First executable goal

Build **PM5 Inspector** as the first real part of the final client.

The user should be able to click:

`Connect → Diagnose → Export Report`

without typing a Proxmark command in CMD.

The Inspector should eventually cover:

- device identity
- hardware revision
- USB VID/PID
- ARM firmware
- FPGA
- ESP32/BWM firmware
- memory information
- capabilities
- Wi-Fi/BLE/TCP status where verifiable
- power/battery telemetry where verifiable
- compatibility
- logs
- report export

Internally, the protocol adapter may use a supported command/API mechanism. CLI syntax is an implementation detail, not the user interface.

## ESP32/BWM is first-class

Always consider the ESP32/BWM subsystem separately from the main RFID/ARM firmware.

Track, where supported:

- ESP32 model
- BWM version/build
- MAC
- Wi-Fi
- BLE
- TCP/UDP
- MQTT
- OTA
- free heap
- uptime
- NVS/system state
- readiness and logs

Do not invent fields that the actual firmware cannot provide.

## Power/battery is first-class

Track power separately. Possible fields include external power, battery presence, voltage, charging, current, temperature and percentage. Availability must be determined from the real hardware/firmware.

## Firmware policy

Never silently update firmware.

Before implementing an update action:

1. Identify the exact current firmware.
2. Identify the exact hardware revision.
3. Determine compatibility.
4. Determine the supported backup method.
5. Preserve an original baseline.
6. Require explicit user confirmation.

Do not invent a firmware extraction/dump procedure. Verify the PM5-specific mechanism first.

## Documentation policy

When adding a source file, include a concise header explaining:

- purpose
- why the component exists
- important compatibility constraints
- relevant documentation files

When changing architecture, update README and the relevant docs.

## GitHub/upstream policy

Use the RfidResearchGroup repository as upstream/reference material. Record exact commits/dates for compatibility claims whenever possible because upstream is a moving target.

The project's compatibility database should remain explicit and testable rather than relying on implicit assumptions.

## Current known facts

- A user-owned Proxmark5 exists and will be inspected physically.
- The project is intended for Windows first and Android later.
- The user wants a click-based interface instead of requiring CMD commands.
- The user wants firmware/ESP32/BWM/power information to be visible and documented.
- The user wants a GitHub project that can be handed to another AI without losing project intent.

## Current unknowns

- Exact PM5 hardware revision.
- Exact ARM firmware version.
- Exact FPGA version.
- Exact ESP32/BWM version.
- Exact USB identifiers/driver requirements.
- Exact PM5-specific protocol additions.
- Exact battery/power telemetry available on this hardware.
- Exact supported backup/extraction mechanism.
- Exact state of official Windows/Android applications.

Do not fill these unknowns with guesses.

## Definition of success

The project succeeds when a user can connect a Proxmark5 and obtain a trustworthy, human-readable and machine-readable description of the actual device without knowing any Proxmark CLI commands, while retaining the ability to use the underlying supported functionality through the same client.
