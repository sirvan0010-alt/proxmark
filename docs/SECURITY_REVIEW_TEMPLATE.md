# Security Review — Proxmark5 Control Center

## Scope

What feature, protocol, firmware or automation change is being reviewed?

## Threats

- Unauthorized device access:
- Destructive or irreversible state changes:
- Firmware/package tampering:
- Credential/secret exposure:
- Malformed input / parser abuse:
- Transport/session abuse:
- Unsafe automation:

## Trust boundary

Identify which inputs are untrusted and which operations cross from observation into device state change.

## Required controls

- [ ] Secure default
- [ ] Explicit authorization
- [ ] Explicit confirmation for risky action
- [ ] Dry-run/simulation where practical
- [ ] Recovery / rollback path
- [ ] Audit log
- [ ] Input validation
- [ ] Negative tests / fuzzing where appropriate
- [ ] Clear UI risk/uncertainty indication

## Evidence

List the evidence supporting the security decision. Do not treat CI success as hardware security proof.

## Decision

`APPROVED` / `APPROVED_WITH_CONTROLS` / `BLOCKED` / `NEEDS_HARDWARE_EVIDENCE`

## Hard stop

Describe any operation that must remain unavailable until the missing evidence or control exists.
