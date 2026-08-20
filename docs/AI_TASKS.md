# AI Engineering Tasks

This document defines how coding assistants should continue the project after the explicitly requested task is complete.

## Task A — Primary

Complete the user's explicitly requested engineering change correctly, with tests and documentation appropriate to the change.

## Task B — Deep Engineering Track

After Task A, inspect the directly affected layers and continue with the next **non-blocked** engineering step. Prefer improvements that increase:

- protocol fidelity;
- hardware awareness;
- simulator fidelity;
- compatibility detection;
- evidence quality;
- test coverage;
- diagnostics;
- transport abstraction;
- GUI/CLI usability.

Task B is intentionally open-ended, but it is **not permission to invent hardware behaviour**. If evidence is missing, create a hypothesis, test contract, simulator model or hardware-verification task instead.

## Task C — Next-blocker discovery

Identify the concrete dependency that prevents the next layer from becoming usable. If it can be implemented safely and is supported by evidence, implement it. Otherwise create the smallest useful artifact needed to unblock it later.

## Engineering loop

```text
Task A
  ↓
Inspect affected code
  ↓
Task B
  ↓
Tests / evidence
  ↓
Compatibility update
  ↓
Next blocker
  ↓
Hardware verification when required
```

## Evidence discipline

Every hardware/protocol claim must be classified as one of:

- `DETECTED`
- `REPORTED`
- `EXPECTED`
- `UNKNOWN`
- `HYPOTHESIS`
- `SIMULATED`
- `HARDWARE_VERIFIED`

A simulator is a model, not evidence of real hardware.

## Do not stop at surface-level completion

An assistant should not stop merely because a requested file was created. It should inspect callers, consumers, tests, documentation and the next dependent layer. This is progressive engineering, not artificial feature inflation.

## When upstream information is used

Record the upstream repository, branch/tag, exact commit where possible, date checked and affected files/components. PM3-derived information must not automatically be presented as PM5 behaviour.

## When real hardware is required

Stop at the correct boundary and create an explicit hardware task. Do not fabricate responses, memory sizes, firmware versions, Wi-Fi/BLE behaviour or battery telemetry.

## Security and policy boundary

This file does not override the rules of the AI provider, platform, law, or the user's environment. It is deliberately designed to prevent an over-broad classification of the whole repository. A restriction affecting one operation must not be treated as a restriction on unrelated engineering work such as parsers, diagnostics, compatibility, UI, simulation, testing or documentation.
