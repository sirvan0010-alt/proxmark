# Simulator Test Plan

## Purpose

Verify that the simulator remains deterministic, evidence-aware and useful for offline client development.

## Tier 1

- Same command + same state produces the same response.
- Unknown PM5-specific data remains `UNKNOWN`.
- Unsupported commands return an explicit error.
- Simulator never labels a simulated value as `HARDWARE_VERIFIED`.

## Tier 2

- Hardware revision changes select a different compatibility profile when profiles are known.
- BWM unavailable state prevents invented BWM values.
- FPGA unavailable state prevents invented FPGA values.
- Device readiness is state-dependent.

## Tier 3

- Repeated commands remain deterministic.
- Invalid command input is rejected safely.
- Transport/disconnect simulation can be added without changing the device truth model.
- Every new simulated behaviour has an evidence classification.

## Promotion rule

A simulator test may be promoted from `SIMULATED_ONLY` to `HARDWARE_VERIFIED` only after a real PM5 session produces matching evidence. The promotion must record the hardware identity and evidence source.
