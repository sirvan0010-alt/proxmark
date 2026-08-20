# Pre-Hardware Readiness Gate

**Purpose:** maximize everything that can be completed and verified before the physical Proxmark5 is connected.

## Current rule

The project may implement architecture, contracts, parsers, evidence handling, simulation and offline tests before hardware. It must not invent hardware observations.

The physical PM5 remains the source of truth for hardware identity, actual transport behavior and payload layouts that cannot be established from reliable upstream sources.

## Ready before first connection

### Software foundation

- [x] C#/.NET project foundation
- [x] Core protocol abstractions
- [x] BWM frame codec
- [x] BWM stream parser/resynchronization
- [x] CRC implementation and protocol evidence documentation
- [x] response correlation primitives
- [x] asynchronous event dispatcher
- [x] read-only BWM adapter foundation
- [x] diagnostic truth model
- [x] evidence-aware diagnostic report model
- [x] JSON report export
- [x] compatibility registry/loader
- [x] offline simulator foundation

### Documentation

- [x] architecture
- [x] CI policy
- [x] hardware baseline checklist
- [x] first hardware session checklist
- [x] protocol notes
- [x] research queue
- [x] simulator contract
- [x] upstream PM3/RRG/Iceman vs PM5 guidance
- [x] human-readable firmware-selection rules

### Safety / integrity

- [x] read-only first hardware session
- [x] no automatic firmware flashing
- [x] no guessed firmware dump
- [x] no silent driver installation
- [x] no conversion of `UNKNOWN`, `EXPECTED`, `HYPOTHESIS` or `SIMULATED` into `DETECTED`
- [x] firmware selection requires established hardware compatibility

## Deliberately blocked until hardware

These are not missing because of poor planning; they require physical evidence:

- [ ] actual USB VID/PID
- [ ] actual USB interface layout
- [ ] actual PM5 hardware revision
- [ ] actual ARM firmware identifier
- [ ] actual FPGA identifier
- [ ] actual BWM/ESP32 firmware identifier
- [ ] actual payload byte layouts where upstream evidence is insufficient
- [ ] actual transport timing/latency
- [ ] actual supported USB/BWM command behavior
- [ ] actual power/battery telemetry

## What happens immediately after connection

```text
1. Enumerate USB
2. Record VID/PID/interfaces
3. Identify transport without writing to device
4. Establish a read-only session
5. Query only verified diagnostic commands
6. Capture raw request/response evidence
7. Decode and classify each value
8. Build DiagnosticReport
9. Compare against compatibility registry
10. Produce human-readable PM3/PM5/BWM explanation
11. Only then decide what implementation work remains
```

## USB transport decision

The USB transport abstraction is already part of the architecture. A full device-specific implementation should be completed only after the first enumeration establishes the actual PM5 USB interface(s).

This prevents building a transport around assumptions such as a particular COM port, CDC layout, VID/PID, baud rate or endpoint arrangement.

## First-session stop conditions

Stop immediately if:

- a tool proposes firmware flashing;
- a command is not known to be read-only;
- the device identity is ambiguous and the next operation would modify state;
- a proposed payload layout is only a hypothesis but the operation would depend on it;
- a backup mechanism is guessed rather than verified.

## Completion criterion

The first hardware session is successful when a reproducible, evidence-backed diagnostic report exists and the device state has not been intentionally modified.
