# Deep Repository Audit — 2026-08-21

## Audit target

Repository: `sirvan0010-alt/proxmark`

Audit scope:

- repository integrity;
- protocol/CRC layer;
- BWM read-only adapter;
- simulator;
- diagnostics and compatibility model;
- CI configuration;
- firmware/upstream guidance;
- readiness for the first physical PM5 session.

## Integrity baseline

Previous audited HEAD: `5bf1bbe9cffe398c7d46755519009db73f9e06ce`

This audit adds commits using normal GitHub file commits. No force-push, reset, history rewrite or squash is used by this audit.

## Protocol layer

### CRC

The BWM CRC implementation is treated as protocol-level evidence only. The CRC algorithm and CRC scope must remain tied to exact upstream source references recorded in `docs/BWM_PROTOCOL.md`.

Required distinction:

- `PROTOCOL VERIFIED` = supported by source-level protocol evidence;
- `HARDWARE VERIFIED` = observed and reproduced on the physical PM5.

The first state does not imply the second.

### Command definitions

`BwmCommandCode.cs` is generated/maintained from the documented upstream BWM command definitions. Command names and numeric IDs must retain provenance.

### Read-only adapter

`BwmReadOnlyAdapter` uses an explicit allow-list rather than a naming convention. Mutating commands must be rejected before the transport is called.

Cancellation is deliberately observable: `OperationCanceledException` must not be converted into an ordinary protocol failure.

## Simulator

The simulator is an offline behavioural model, not a source of hardware facts.

Current design goals:

- deterministic responses;
- consistent state across related queries;
- valid BWM framing;
- realistic failure categories without pretending to know undocumented PM5 behaviour;
- fault injection for transport/protocol tests;
- clear `SIMULATED` provenance.

## Diagnostics

The truth model remains explicit:

`DETECTED / REPORTED / EXPECTED / UNKNOWN / HYPOTHESIS / SIMULATED`

No compatibility registry entry is allowed to become a detected hardware fact merely because it matches a query.

## Compatibility

The hardware and firmware registries remain intentionally conservative. Unknown PM5 revision/VID/PID values must remain unknown until the physical device provides evidence.

The firmware routing pipeline is:

```text
Detect → identify family/revision → identify current firmware
→ compare compatibility → explain result → show established candidates
```

## Upstream separation

The repository now documents three distinct reference categories:

1. historical Proxmark3 reference;
2. RfidResearchGroup/Iceman Proxmark3 ecosystem;
3. RfidResearchGroup Proxmark5 BWM/ESP32 source.

A PM3 source observation must not be promoted to a PM5 hardware claim.

## CI

GitHub Actions is configured for Ubuntu and Windows with .NET 10 restore/build/test and TRX artifacts.

CI status is only considered `CI VERIFIED` when an actual workflow run for the exact commit has completed successfully. Static inspection of workflow YAML is not CI verification.

## Pre-hardware boundary

The following are intentionally deferred until the PM5 is physically connected:

- USB VID/PID and interface confirmation;
- exact driver requirement;
- actual PM5 hardware revision;
- actual ARM firmware version;
- actual FPGA version;
- actual ESP32/BWM version;
- actual BWM payload layouts;
- actual battery/power telemetry;
- transport framing behaviour observed from the physical device.

## Exit criterion before hardware

The repository is considered ready for the first hardware session when:

- the build/test workflow is configured;
- simulator tests cover success and failure paths;
- read-only protocol code cannot issue mutating commands;
- reports preserve evidence provenance;
- compatibility data contains no invented hardware facts;
- the first-session checklist is available;
- firmware selection remains disabled until hardware identity is established.

The hardware session then becomes a controlled evidence-collection step rather than an exploratory guess-and-flash operation.
