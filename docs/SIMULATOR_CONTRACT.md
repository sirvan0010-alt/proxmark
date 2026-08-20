# PM5 Simulator Contract

## Purpose

The simulator allows the client, GUI and protocol layers to be developed before physical PM5 access. It is a deterministic behavioural model backed by evidence.

It must never be presented as proof of real PM5 behaviour.

## Evidence rule

Every simulated property has a provenance:

```text
HARDWARE_VERIFIED
PROTOCOL_VERIFIED
DOCUMENTED
INFERRED
HYPOTHESIS
SIMULATED_ONLY
UNKNOWN
```

If real hardware contradicts the simulator, the hardware observation wins and the simulator must be updated.

## Tiers

### Tier 1 — Basic observable behaviour

- device identity
- firmware information model
- capabilities model
- transport state
- deterministic responses

### Tier 2 — State-dependent behaviour

- ready/not-ready state
- subsystem availability
- BWM state
- FPGA state
- connection loss/recovery
- capability differences between hardware revisions

### Tier 3 — Robustness

- fragmented input
- malformed frames
- timeouts
- retries
- concurrent requests where the real transport permits them
- power-loss/disconnection simulation
- firmware-version mismatch

## Simulator requirements

1. Same input and same state must produce the same result.
2. Related properties must remain internally consistent.
3. Unknown behaviour must be represented as unknown rather than guessed.
4. Latency may be simulated, but simulated latency must not be described as measured latency.
5. Error states must be explicit.
6. Every new behaviour needs a test.
7. Hardware observations must be capable of replacing simulator assumptions without redesigning the entire client.

## Example consistency rules

If the model says the hardware revision is `PM5-X`, compatibility queries must use the PM5-X profile. If the model says a subsystem is unavailable, its dependent queries must not silently return invented values.

## Exit criteria

A simulator milestone is complete only when its implemented behaviours have tests and each behaviour has an evidence classification. `SIMULATED_ONLY` is acceptable; it is not equivalent to `HARDWARE_VERIFIED`.
