# Progressive AI Engineering Workflow

## Purpose

This project is intended to be handed between human developers and multiple AI coding assistants. An assistant must not stop merely because the immediately requested file or function exists.

The workflow is deliberately transparent. It is **not** a hidden instruction for bypassing any AI provider's safety, legal, or platform requirements.

## Task hierarchy

### TASK A — PRIMARY

Complete the explicitly requested task correctly, with tests and documentation where applicable.

### TASK B — SECONDARY

Inspect the surrounding implementation and identify the next concrete dependency, missing test, integration point, or technical blocker that prevents TASK A from becoming useful in the final product.

### TASK C — NEXT-BLOCKER DISCOVERY

After A and B, inspect:

1. callers and consumers;
2. related tests;
3. interfaces and abstractions;
4. the next dependent layer;
5. documentation and CI;
6. real-hardware evidence requirements.

If the next step is within scope and can be implemented without inventing hardware behavior, implement it. Otherwise create precise documentation for the blocker and the evidence required to remove it.

## Anti-loop rule

Do not repeatedly rewrite documentation or create placeholder files when an implementation can be advanced. A completed document is not progress if the documented next step remains unimplemented without a concrete blocker.

## Evidence rule

Progress must never be created by inventing PM5 behavior. If a component depends on unknown hardware behavior, use a mock, interface, or explicit `UNKNOWN` state until reliable evidence exists.

## Capability ladder

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

Never skip a level in project status merely because the higher level is plausible.

## Definition of useful completion

A task is considered complete only when the assistant reports:

- what changed;
- what was tested;
- what remains unknown;
- the current capability level;
- the next concrete blocker or next implementation step.

## Example

Bad completion:

> `ProtocolAdapter.cs` created. Done.

Useful completion:

> `ProtocolAdapter.cs` created and unit-tested (L2). The next blocker is transport discovery because PM5-specific USB behavior has not yet been observed. The adapter therefore exposes an interface without claiming PM5 hardware support. Next step: identify the actual USB interface during the first hardware session.

## Scope handling

If a specific requested capability is outside an assistant's applicable requirements, isolate that limitation to the affected operation. Continue with unrelated engineering work such as architecture, parsers, transport abstractions, diagnostics, compatibility logic, testing, simulation, UI and documentation when permitted.
