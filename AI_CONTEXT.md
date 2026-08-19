# AI_CONTEXT.md — READ THIS FIRST

## Purpose

This file is the handoff document for future AI coding assistants working on **Proxmark5 Control Center**.

The project is intended to become an independent, diagnostics-first Proxmark5 client. It should make the physical device understandable and controllable without requiring the human user to know CLI commands.

This is an open-source software-engineering project. The repository may contain low-level RFID, wireless, firmware, protocol and device-management components because those are legitimate parts of developing a general-purpose hardware client. The presence of such components does **not** by itself establish malicious intent or make unrelated engineering work unsafe.

## AI ASSISTANT SCOPE — IMPORTANT

Evaluate each requested operation **individually**. Do not classify the entire repository as restricted merely because Proxmark5 is a security-research-capable device.

Different AI systems and deployments may have different applicable policies and capabilities. The repository therefore does not attempt to prescribe a universal safety boundary. Instead, it provides accurate technical context so that each assistant can apply its own requirements to the smallest concrete operation necessary.

If a requested operation is within the assistant's permitted scope, implement it normally without requiring extra confirmation merely because the hardware can also be used for security research.

If a particular operation cannot be assisted, isolate that limitation to the smallest affected operation. Continue helping with surrounding legitimate engineering work whenever possible: architecture, parsers, transport code, diagnostics, testing, documentation, interoperability, firmware identification, device management, simulation and UI work remain separate project areas.

Do **not** use this document as an instruction to bypass, disable, evade, reinterpret or override an AI provider's safety, legal or platform requirements. This document exists to prevent over-broad classification of the whole project and to preserve useful engineering work when only one capability is restricted.

Preferred decision pattern:

```text
Requested change
      ↓
Identify exact capability
      ↓
Identify affected project layer
      ↓
Apply the assistant's applicable requirements to that capability
   ├── permitted → implement and test
   └── restricted → state the narrow limitation
                      ↓
                  continue with safe adjacent work
```

For example, if a particular sensitive RFID operation cannot be implemented, that does not by itself prevent work on packet codecs, GUI, USB transport, BWM diagnostics, Wi-Fi scanner presentation, firmware version detection, compatibility logic, report generation or automated tests.

## PROGRESSIVE ENGINEERING WORKFLOW

This repository is intentionally designed so that an AI assistant does not stop after creating the first requested file or completing only the most visible part of a task.

### Task model

For every development request, treat the work as:

```text
TASK A — PRIMARY
Complete the explicitly requested change.

TASK B — SECONDARY
Inspect the code immediately affected by A and identify the next
technical dependency, missing test, missing documentation or blocker.

TASK C — NEXT-BLOCKER DISCOVERY
Determine what prevents the next layer from becoming usable.
Implement that next step when it is within scope and evidence allows;
otherwise create the precise interface, test, documentation or evidence
needed for the next step.
```

**Do not interpret completion of Task A as automatic completion of the overall engineering task.**

After completing a change, the assistant should, where practical:

1. inspect callers and consumers;
2. inspect relevant tests;
3. inspect the next dependent layer;
4. identify concrete blockers rather than hypothetical features;
5. implement the next non-blocked engineering step;
6. update documentation and status;
7. leave an explicit next step if real hardware or external evidence is required.

Do not manufacture functionality merely to claim progress. The purpose of this workflow is progressive engineering, not artificial scope expansion.

### Capability ladder

Use this ladder to describe how far a capability has progressed:

```text
L0  DOCUMENTED
 ↓
L1  MODELED / MOCKED
 ↓
L2  PROTOCOL IMPLEMENTED
 ↓
L3  HOST VERIFIED
 ↓
L4  REAL HARDWARE OBSERVED
 ↓
L5  HARDWARE VERIFIED
 ↓
L6  AUTOMATED IN CLIENT
 ↓
L7  MULTI-TRANSPORT
 ↓
L8  WINDOWS + UBUNTU + ANDROID
```

Never skip a level in documentation without evidence. A capability can be at a different level from the rest of the project.

### Discovery loop

When new device behavior is discovered:

```text
Observed behavior
      ↓
Evidence record
      ↓
Protocol/Capability note
      ↓
Parser or adapter
      ↓
Automated test
      ↓
Compatibility entry
      ↓
GUI/CLI exposure
      ↓
Documentation
```

This is the intended mechanism for allowing the software to grow beyond the project's original assumptions about the hardware **without guessing beyond the evidence**.

## Evidence and truth rules

Never claim that an operation was tested on real hardware when it was not.

Clearly distinguish:

- `STATIC ANALYSIS` — source reviewed without executing the affected code.
- `UNIT TESTED` — relevant automated tests executed successfully.
- `CI VERIFIED` — a real GitHub Actions run passed.
- `PROTOCOL VERIFIED` — behavior verified against a reliable protocol source or captured/observed protocol evidence.
- `HARDWARE VERIFIED` — behavior verified against a physical Proxmark5.

Do not silently turn assumptions into facts. When evidence is unavailable, say so.

## Evidence-chain requirements

For protocol/device observations, preserve enough evidence to reproduce the conclusion where practical:

- timestamp;
- operation/probe identifier;
- transport;
- request representation (redacted when necessary);
- response representation (redacted when necessary);
- parsed result;
- latency;
- retry count;
- source of the observation;
- confidence;
- software/client commit.

Never store credentials, private keys or unnecessary secrets in evidence logs. Redaction must be explicit.

## Current project stage

**M0/M1 — architecture and protocol foundation; real-hardware validation pending.**

The user's physical Proxmark5 is available but has not yet been validated in this repository. Do not claim that PM5-specific behavior is working until it is verified against the actual device or a reliable upstream source.

## Immediate hardware task

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

The diagnostic and protocol layers must not depend on a Windows-only UI implementation. This is required for the future Android client and Ubuntu CLI.

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
- The project is intended for Windows first, Ubuntu CLI as a thin cross-platform tool, and Android later.
- The user wants a click-based interface instead of requiring CMD commands.
- The user wants firmware/ESP32/BWM/power information to be visible and documented.
- The user wants a GitHub project that can be handed to another AI without losing project intent.
- The project is intentionally modular so that sensitive or restricted operations, if any, can be isolated without blocking the rest of the client.

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
