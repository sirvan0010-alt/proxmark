# Proxmark5 Control Center

> Independent, diagnostics-first client and device-management suite for Proxmark5.

## Why this project exists

The current Proxmark ecosystem is evolving quickly. Much of the established software and documentation lives in the RfidResearchGroup `proxmark3` repository, while Proxmark5 introduces additional hardware and an ESP32/BWM subsystem. This can make it difficult to determine what is actually supported by a particular physical device, firmware build, hardware revision, or transport.

This project exists to provide a **simple, explicit and hardware-aware client for Proxmark5** without requiring the user to know command-line syntax.

The first objective is not to recreate every RFID command. The first objective is to make the device understandable, diagnosable and safely manageable.

## Core goals

1. Detect a connected Proxmark5 automatically.
2. Identify the actual hardware and hardware revision.
3. Read and report ARM/FPGA/ESP32/BWM firmware information.
4. Inspect available capabilities and transports.
5. Inspect memory and other hardware properties where the protocol permits reliable verification.
6. Distinguish **DETECTED**, **REPORTED**, **EXPECTED**, **HYPOTHESIS**, **SIMULATED** and **UNKNOWN** information instead of guessing.
7. Calculate an explicit confidence level for important diagnostic results.
8. Check hardware/firmware/client compatibility.
9. Provide diagnostics without requiring CMD/terminal commands from the user.
10. Create complete, versioned diagnostic reports suitable for humans and AI assistants.
11. Provide safe backup workflows before firmware changes.
12. Manage USB, Wi-Fi/TCP and Bluetooth/BLE connectivity where supported by the actual device.
13. Eventually provide a modern Windows GUI and Android application using shared protocol/core code.
14. Eventually expose the normal supported Proxmark functionality through a graphical interface and automation layer.
15. Keep compatibility information tied to exact firmware versions, commits and dates.
16. Explain PM5 versus PM3/reference hardware differences in human-readable terms before firmware selection.

## Non-goals and safety rules

- Do **not** assume Proxmark3 and Proxmark5 are identical.
- Do **not** assume that every function in the RRG `proxmark3` tree works on Proxmark5.
- Do **not** silently flash or replace firmware.
- Do **not** overwrite an original backup.
- Do **not** invent hardware information when it cannot be verified.
- Prefer documented protocols/APIs over parsing terminal screen output.
- Do not modify the Proxmark firmware merely to make this client work.
- Keep legacy PM3 protocol support separate from PM5/BWM-specific support.
- Every compatibility claim should record the exact source version/commit/date when possible.
- Firmware update actions must be explicit and require confirmation.

## Diagnostic truth model

For important values the client should show the source of the information:

| State | Meaning |
|---|---|
| `DETECTED` | Directly detected or verified by a reliable hardware/protocol operation. |
| `REPORTED` | Reported by the connected firmware/device. |
| `EXPECTED` | Expected from a known hardware/firmware compatibility definition. |
| `HYPOTHESIS` | Proposed behaviour not yet verified. |
| `SIMULATED` | Behaviour represented by the offline simulator only. |
| `UNKNOWN` | Not currently determinable with sufficient confidence. |

The client must never silently convert `EXPECTED`, `HYPOTHESIS` or `SIMULATED` into `DETECTED`.

## Human-readable hardware and firmware guidance

The application should not force the user to interpret VID/PID values or repository names. It should explain:

- which hardware family is connected;
- exact hardware revision when detectable;
- current ARM/FPGA/BWM versions;
- which PM3-compatible components are reusable;
- which functions are PM5-specific;
- why a firmware candidate is compatible, incompatible or unknown.

Firmware selection must follow:

```text
Detect device
    ↓
Identify family + exact revision
    ↓
Identify current firmware/subsystems
    ↓
Compare compatibility definitions
    ↓
Explain result to human
    ↓
Offer only established firmware candidates
```

See `docs/HARDWARE_COMPARISON.md`, `docs/CLI_COMMAND_REFERENCE.md`, `compatibility/hardware.json` and `compatibility/firmware.json`.

## CLI command reference

The project keeps a separate evidence-labelled reference of the established PM3 command surface and the still-unverified PM5 command surface:

- `docs/CLI_COMMAND_REFERENCE.md`

This distinction is important: a documented PM3 command such as `hw version` is not automatically a PM5 command, and BWM command IDs are protocol-level identifiers rather than user-facing CLI commands.

## First milestone: PM5 Inspector

The first executable milestone is a real client foundation called **PM5 Inspector**. It is not a disposable script. It is the first component of the final PM5 Control Center.

The first UI should provide, at minimum:

- Connect / Disconnect
- Automatic device detection
- Hardware identity
- USB identity (VID/PID and transport information where available)
- ARM firmware information
- FPGA information
- ESP32/BWM information
- Memory information
- Wi-Fi/BLE/TCP capability status where available
- Power/battery information where the device exposes it
- Human-readable PM3/PM5 hardware comparison
- Compatibility result
- Diagnostic log
- Export full diagnostic report
- Backup workflow entry point

The user should be able to obtain these details by clicking buttons. The user must not need to type Proxmark CLI commands into CMD.

## Planned architecture

```text
PM5 Control Center
        |
        +-- UI (Windows / Android)
        |
        +-- Device Inspector
        |
        +-- Compatibility Engine
        |
        +-- Diagnostic / Report Engine
        |
        +-- Backup / Firmware Manager
        |
        +-- Connection Manager
        |      +-- USB
        |      +-- Serial/transport as applicable
        |      +-- Wi-Fi/TCP
        |      +-- Bluetooth/BLE
        |
        +-- Protocol Abstraction
               +-- Legacy PM3-compatible protocol
               +-- PM5-specific protocol
               +-- ESP32/BWM API
               |
               +-- Proxmark5
```

The protocol layer is deliberately separated from the UI so that the same core can later be used by Windows, Ubuntu CLI and Android.

## Hardware areas that must be tracked

### Main device

- hardware model/revision
- serial/device identifiers where available
- USB VID/PID
- CPU/ARM firmware
- FPGA firmware/image
- memory and addressable regions where safely verifiable
- RF subsystem capabilities

### ESP32 / BWM subsystem

Treat the ESP32/BWM subsystem as a first-class component, not as a footnote. Track where supported:

- ESP32 model
- BWM firmware version/build
- MAC address
- Wi-Fi state/configuration
- BLE state/configuration
- TCP/UDP capability/configuration
- MQTT capability/configuration
- OTA capability/status
- free heap/RAM
- uptime/timestamp
- NVS/system information
- BWM logs and readiness state

A capability must be marked unsupported/unknown if the connected firmware does not expose a reliable way to verify it.

### Power and battery

Track power information separately. Depending on the actual Proxmark5 hardware and exposed telemetry, this may include external power, battery presence, voltage, current/charge state, charging state, temperature, percentage and warnings. Report `UNKNOWN` when telemetry cannot be verified.

## Firmware and backup policy

Before any firmware update is attempted, the client should identify the current device state and create or verify a backup when the hardware/protocol provides a supported backup mechanism.

**Do not implement a guessed firmware-dump command.** The backup implementation must be based on verified PM5-compatible mechanisms.

## Compatibility database

```text
compatibility/
  hardware.json
  firmware.json
  bwm.json
  protocols.json
  known-issues.json
```

Compatibility definitions must remain explicit and evidence-backed.

## AI engineering workflow

The project uses a progressive engineering model rather than stopping after the first visible task:

```text
Task A — requested change
        ↓
Task B — deep engineering of affected layers
        ↓
Tests / evidence
        ↓
Compatibility update
        ↓
Next blocker
        ↓
Real hardware verification when required
```

See:

- `AI_CONTEXT.md`
- `docs/AI_TASKS.md`
- `docs/SIMULATOR_CONTRACT.md`
- `docs/RESEARCH_LOG.md`
- `docs/RESEARCH_QUEUE.md`
- `docs/SIMULATOR_TEST_PLAN.md`

## GitHub/upstream policy

The RfidResearchGroup Proxmark3 repository is an upstream reference and an important source of protocol/client knowledge. It is not the sole definition of Proxmark5 support.

Because upstream changes frequently, compatibility claims should record repository, branch/tag, exact commit when possible, date checked and relevant files/components.

## Current status

**Stage: Project foundation / real-hardware validation pending.**

The physical Proxmark5 owned by the project maintainer has not yet been inspected in this repository. The next hardware session should be read-only first: identify USB, inspect device/firmware versions, determine available PM5/BWM interfaces, and preserve the original state before any firmware update.

## Project principle

**First make the device observable and trustworthy. Then make it easy to control.**
