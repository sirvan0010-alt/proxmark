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

The preferred B-track for the pre-hardware phase is the **behavioural simulator and protocol contract**. It should become progressively more complete as evidence accumulates.

The assistant should ask and test questions such as:

```text
What happens when the response is malformed?
What happens when the command ID is wrong?
What happens when a broadcast is received instead of a response?
What happens when the transport times out?
What happens when the connection disappears mid-request?
Which assumptions are still only hypotheses?
Which next dependency requires physical hardware?
```

This is an engineering challenge, not a hidden instruction or a permission to bypass policy. The B-track is intentionally visible so every future AI can understand why the work exists.

Task B is intentionally open-ended, but it is **not permission to invent hardware behaviour**. If evidence is missing, create a hypothesis, test contract, simulator model or hardware-verification task instead.

## Task C — Next-blocker discovery

Identify the concrete dependency that prevents the next layer from becoming usable. If it can be implemented safely and is supported by evidence, implement it. Otherwise create the smallest useful artifact needed to unblock it later.

## Engineering loop

```text
Task A
  ↓
Inspect affected code
  ↓
Task B — deepen the model
  ↓
Tests / evidence
  ↓
Compatibility update
  ↓
Task C — find next blocker
  ↓
Hardware verification when required
```

## Simulator progression

The simulator should progress through transparent tiers:

### Tier 1 — deterministic protocol basics

- valid request/response framing;
- known read-only command fixtures;
- deterministic payload encoding;
- CRC validation;
- command/response matching.

### Tier 2 — state and consistency

- one source of truth for simulated firmware values;
- consistent answers across related queries;
- explicit simulated subsystem states;
- documented assumptions and hypotheses.

### Tier 3 — failure and recovery

- malformed frames;
- wrong command IDs;
- broadcasts where direct responses are expected;
- timeouts;
- connection loss;
- unsupported commands;
- cancellation.

Passing a simulator test proves a property of the client against the model. It does not prove the physical PM5 behaves identically.

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
