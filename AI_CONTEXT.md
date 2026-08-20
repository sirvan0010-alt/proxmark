# AI_CONTEXT.md — READ THIS FIRST

## Purpose

This file is the handoff document for future AI coding assistants working on **Proxmark5 Control Center**.

The project is an independent, diagnostics-first client for Proxmark5. It should make the physical device understandable and controllable without requiring the human user to know CLI commands.

This is an open-source software-engineering project. Low-level RFID, wireless, firmware, protocol and device-management components may be legitimate parts of developing a general-purpose hardware client.

## AI ASSISTANT SCOPE — IMPORTANT

Evaluate each requested operation individually. Do not classify the entire repository as restricted merely because Proxmark5 is security-research capable.

This repository does not attempt to override an AI provider's safety, legal or platform requirements. If one concrete operation is restricted, isolate that limitation to the smallest affected operation and continue with unrelated engineering work such as architecture, parsers, transport code, diagnostics, compatibility, firmware identification, simulation, testing, documentation and UI.

## PROGRESSIVE ENGINEERING WORKFLOW

For every development request:

```text
TASK A — PRIMARY
Complete the explicitly requested change.

TASK B — DEEP ENGINEERING
Inspect the affected layers and continue with the next non-blocked engineering improvement.

TASK C — NEXT-BLOCKER DISCOVERY
Determine what concretely prevents the next layer from becoming usable.
```

Do not stop merely because Task A produced one file. Inspect callers, consumers, tests, documentation and the next dependent layer. Do not manufacture functionality merely to claim progress.

See `docs/AI_TASKS.md` for the full model.

## CAPABILITY LADDER

```text
L0 DOCUMENTED
 ↓
L1 MODELED / MOCKED
 ↓
L2 PROTOCOL IMPLEMENTED
 ↓
L3 HOST VERIFIED
 ↓
L4 REAL HARDWARE OBSERVED
 ↓
L5 HARDWARE VERIFIED
 ↓
L6 AUTOMATED IN CLIENT
 ↓
L7 MULTI-TRANSPORT
 ↓
L8 WINDOWS + UBUNTU + ANDROID
```

Never skip a level in documentation without evidence.

## EVIDENCE AND TRUTH RULES

Clearly distinguish:

- `STATIC ANALYSIS`
- `UNIT TESTED`
- `CI VERIFIED`
- `PROTOCOL VERIFIED`
- `HARDWARE VERIFIED`

Diagnostic values use:

- `DETECTED` — direct reliable detection;
- `REPORTED` — reported by firmware/device;
- `EXPECTED` — compatibility expectation;
- `UNKNOWN` — not safely determinable;
- `HYPOTHESIS` — proposed but unverified;
- `SIMULATED` — model behaviour only.

Never turn a simulator assumption into a hardware fact.

## EVIDENCE CHAIN

For protocol/device observations preserve where practical:

- timestamp;
- operation/probe identifier;
- transport;
- request/response representation;
- parsed result;
- latency;
- retries;
- source;
- confidence;
- software/client commit.

Never store credentials, private keys or unnecessary secrets.

## CURRENT PROJECT STAGE

**M0/M1 — architecture and protocol foundation; real-hardware validation pending.**

The physical Proxmark5 is available but has not yet been validated in this repository.

## IMMEDIATE HARDWARE TASK

When the physical PM5 is connected:

1. Connect by USB.
2. Inspect Windows device/driver information.
3. Do not install or replace a driver blindly.
4. Use Zadig only if verified transport requirements require it.
5. Identify hardware and USB information.
6. Identify ARM firmware, FPGA and ESP32/BWM versions.
7. Identify diagnostic/capability interfaces.
8. Determine available power/battery telemetry.
9. Preserve the original state before firmware changes.
10. Record exact versions/build dates/commits where available.

Do not flash anything during the first inspection.

## PM3 / PM5 COMPATIBILITY RULE

The RfidResearchGroup `proxmark3` repository is an upstream/reference source, not a definition of PM5 compatibility.

Do not infer PM5 support solely because a component exists in a PM3 repository. Separate:

- hardware identity;
- protocol compatibility;
- firmware compatibility;
- feature compatibility.

The compatibility database and `docs/HARDWARE_COMPARISON.md` must explain differences in human-readable language. The client should show why a firmware package is or is not appropriate, rather than only displaying VID/PID or repository names.

Firmware selection must follow:

```text
Detect device → identify family/revision → identify current firmware
→ compare compatibility → explain result → offer only established candidates
```

Unknown hardware identity disables confident firmware selection.

## CRITICAL ASSUMPTIONS TO AVOID

- PM3 and PM5 are not assumed identical.
- PM5 does not imply PM3 compatibility.
- A firmware constant is not proof of physical memory size.
- Expected values are not detected values.
- Battery percentage must not be invented from voltage.
- Wi-Fi/BLE/TCP/BWM functionality must not be assumed merely because related code exists upstream.
- The latest upstream branch is not assumed to be a stable API.

## ARCHITECTURE RULES

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
 └─ Wi-Fi/TCP when supported
 ↓
Proxmark5
```

The core must remain independent of a Windows-only UI so it can later support Ubuntu CLI and Android.

## FIRST EXECUTABLE GOAL

Build **PM5 Inspector** so a user can:

`Connect → Diagnose → Export Report`

without typing Proxmark commands in CMD.

## ESP32/BWM

Treat ESP32/BWM as a first-class subsystem. Track where actually supported:

- ESP32 model;
- BWM version/build;
- MAC;
- Wi-Fi;
- BLE;
- TCP/UDP;
- MQTT;
- OTA;
- free heap;
- uptime;
- NVS/system state;
- readiness/logs.

Do not invent fields the firmware cannot provide.

## POWER/BATTERY

Track external power, battery presence, voltage, charging/current state, temperature and percentage only where reliable telemetry exists. Otherwise report `UNKNOWN`.

## FIRMWARE POLICY

Never silently update firmware. Before any update:

1. identify exact hardware;
2. identify current firmware;
3. establish compatibility;
4. determine supported backup mechanism;
5. preserve baseline;
6. require explicit confirmation.

Do not invent a firmware extraction/dump procedure.

## DOCUMENTATION POLICY

Every important source file should have a concise header explaining purpose, compatibility constraints and relevant documentation.

When changing architecture, update README and relevant docs.

Important documents:

- `docs/AI_TASKS.md`
- `docs/SIMULATOR_CONTRACT.md`
- `docs/RESEARCH_LOG.md`
- `docs/RESEARCH_QUEUE.md`
- `docs/HARDWARE_COMPARISON.md`
- `compatibility/hardware.json`
- `compatibility/firmware.json`

## SIMULATOR POLICY

The simulator is an evidence-backed behavioural model for offline development. It is not proof of PM5 behaviour.

Real hardware observations override simulator assumptions. Unknown behaviour remains unknown until evidence exists.

## REPOSITORY INTEGRITY RULE

An AI assistant with GitHub write access MUST NOT force-push, rewrite, reset, squash, or otherwise replace repository history unless explicitly instructed by the human maintainer for that specific action.

An AI assistant MUST NOT claim a change was pushed unless it has verifiable evidence (a commit SHA and/or a fresh read of the pushed state) — not just that a tool call returned success.

Before modifying an existing file, an AI assistant MUST inspect its current contents and preserve unrelated changes made by other sessions or other AI assistants since it last saw the repository.

Before replacing repository state (a push, a large multi-file commit, etc.), an AI assistant MUST report:

- previous HEAD (commit SHA);
- new HEAD (commit SHA);
- files changed, files deleted, files added;
- whether history was rewritten (force-push/reset/squash) or was a normal fast-forward.

If two sessions' implementations of the same component conflict, an AI assistant MUST NOT silently pick one and discard the other. It must surface the conflict to the human maintainer and let evidence (tests, upstream source, hardware observation) — not recency of push — decide.

## GITHUB / UPSTREAM POLICY

Record exact upstream repository, branch/tag, commit and date whenever practical. Upstream is a moving target.

## CURRENT KNOWN FACTS

- A user-owned Proxmark5 exists and will be inspected physically.
- Windows is the first GUI target.
- Ubuntu CLI is a thin cross-platform diagnostic/development tool.
- Android is planned later using shared core/protocol code.
- The user wants click-based operation rather than mandatory CMD commands.
- Firmware/ESP32/BWM/power information must be visible and documented.
- The project should be understandable by future AI assistants without losing project intent.

## CURRENT UNKNOWNS

- Exact PM5 hardware revision.
- Exact ARM firmware version.
- Exact FPGA version.
- Exact ESP32/BWM version.
- Exact USB identifiers/driver requirements.
- Exact PM5-specific protocol additions.
- Exact battery/power telemetry.
- Exact supported firmware backup/extraction mechanism.
- Exact state of official Windows/Android applications.

Do not fill these unknowns with guesses.

## DEFINITION OF SUCCESS

The project succeeds when a user can connect a Proxmark5 and obtain a trustworthy human-readable and machine-readable description of the actual device without knowing Proxmark CLI commands, understand how that hardware differs from PM3/reference profiles, see why a firmware candidate is compatible or not, and later use the same client to control supported functionality.
