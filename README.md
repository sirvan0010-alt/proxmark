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
6. Distinguish **DETECTED**, **REPORTED**, **EXPECTED** and **UNKNOWN** information instead of guessing.
7. Calculate an explicit confidence level for important diagnostic results.
8. Check hardware/firmware/client compatibility.
9. Provide diagnostics without requiring CMD/terminal commands from the user.
10. Create complete, versioned diagnostic reports suitable for humans and AI assistants.
11. Provide safe backup workflows before firmware changes.
12. Manage USB, Wi-Fi/TCP and Bluetooth/BLE connectivity where supported by the actual device.
13. Eventually provide a modern Windows GUI and Android application using shared protocol/core code.
14. Eventually expose the normal Proxmark functionality through a graphical interface and automation layer.
15. Keep compatibility information tied to exact firmware versions, commits and dates.

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
| `UNKNOWN` | Not currently determinable with sufficient confidence. |

The client must never silently convert `EXPECTED` into `DETECTED`.

Example:

```text
Memory
  Firmware reports: 512 KiB       REPORTED
  Hardware expected: 512 KiB      EXPECTED
  Direct verification: 512 KiB    DETECTED
  Result: VERIFIED / HIGH CONFIDENCE
```

If the values disagree, the application must explain the mismatch instead of hiding it.

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
- Compatibility result
- Diagnostic log
- Export full diagnostic report
- Backup workflow entry point

The user should be able to obtain these details by clicking buttons. The user must not need to type Proxmark CLI commands into CMD.

Internally, the application may use the existing protocol/API or command mechanisms when that is the correct supported interface. Such commands are implementation details hidden behind the protocol abstraction layer.

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

The protocol layer is deliberately separated from the UI so that the same core can later be used by Windows and Android.

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

Track power information separately. Depending on the actual Proxmark5 hardware and exposed telemetry, this may include:

- external power presence
- battery presence
- battery voltage
- current/charge state
- charging state
- temperature
- battery percentage if a reliable fuel-gauge value exists
- power warnings

Do not assume that percentage, current or temperature are available merely because the hardware contains a battery. Report `UNKNOWN` when telemetry cannot be verified.

## Firmware and backup policy

Before any firmware update is attempted, the client should identify the current device state and create or verify a backup when the hardware/protocol provides a supported backup mechanism.

Backups must be immutable from the normal update workflow and should contain:

- device identification
- hardware revision
- ARM firmware information/data where safely extractable
- FPGA information/data where safely extractable
- ESP32/BWM firmware information/data where safely extractable
- other supported flash/configuration data
- timestamp
- exact client version
- exact upstream firmware commit/version where known

**Do not implement a guessed firmware-dump command.** The backup implementation must be based on verified PM5-compatible mechanisms.

## Compatibility database

The project will maintain explicit compatibility definitions for hardware revisions and firmware generations. Compatibility is not based solely on the repository name `proxmark3`.

Future examples:

```text
compatibility/
  hardware.json
  firmware.json
  bwm.json
  protocols.json
  known-issues.json
```

A new PM5 revision should normally be added as a compatibility definition rather than forcing unrelated parts of the application to be rewritten.

## GitHub/upstream policy

The RfidResearchGroup Proxmark3 repository is an upstream reference and an important source of protocol/client knowledge. It is not the sole definition of Proxmark5 support.

Because the upstream project changes frequently, compatibility claims must record:

- repository
- branch/tag when relevant
- exact commit when possible
- date checked
- relevant files/components

Do not write documentation that assumes `master`/`main` will remain structurally identical.

## Documentation for future AI assistants

Before modifying this project, an AI coding assistant should read:

1. `README.md`
2. `AI_CONTEXT.md`
3. `docs/ARCHITECTURE.md`
4. `docs/COMPATIBILITY.md`
5. relevant source-file headers

The AI must preserve the design rules above and must not invent unverified PM5 behavior.

## Current status

**Stage: Project foundation / real-hardware validation pending.**

The physical Proxmark5 owned by the project maintainer has not yet been inspected in this repository. The next hardware session should be read-only first: identify USB, inspect device/firmware versions, determine available PM5/BWM interfaces, and preserve the original state before any firmware update.

## Planned milestones

### M0 — Documentation and architecture
- project specification
- AI context
- architecture
- compatibility model
- diagnostic report schema

### M1 — Real hardware inspector
- USB detection
- device identification
- firmware/FPGA/BWM discovery
- capability discovery
- power/battery discovery
- report export

### M2 — Safe backup and compatibility
- verified backup mechanisms
- immutable baseline snapshots
- compatibility database
- mismatch/confidence reporting

### M3 — Windows client
- graphical dashboard
- connection manager
- diagnostics
- logs
- protocol abstraction

### M4 — PM5 functions
- graphical access to supported Proxmark functions
- profiles
- standalone mode management
- automation/workflows

### M5 — Network and wireless
- Wi-Fi/TCP
- BLE
- remote operation where supported
- secure network configuration

### M6 — Android
- shared core/protocol layer
- Android UI
- Wi-Fi/BLE/USB OTG where supported
- remote operation through a secure network path

## Project principle

**First make the device observable and trustworthy. Then make it easy to control.**
