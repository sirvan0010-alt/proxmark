# Wireless Lab v3.0

Status: implementation-ready and source-integrated.

## Guarantees

- `SupportStatus` is independent from evidence and policy.
- `NotSupported` is never auto-promoted by a hardware test.
- `PolicyStatus.Disabled` records a test but cannot promote evidence or UI exposure.
- UI exposure is recomputed from support, policy and hardware evidence.
- Hardware validation requires a human-confirmed session, exact session-ID binding, device-identity binding, and evidence timestamped after the session start.
- Protocol frames use explicit SOF, command, payload length, CRC-8/CCITT, and EOF boundaries.
- The parser returns `bytesConsumed`; incomplete frames consume zero bytes and malformed frames consume one byte for resynchronization.
- Serial lifecycle is reconnectable: each connection gets a fresh cancellation source and disconnect cancels only the current reader.
- `SendAsync` accepts and checks a `CancellationToken` before transmission.

## Frame format

`AA | CMD | LEN | PAYLOAD[LEN] | CRC8 | 55`

CRC covers `CMD`, `LEN`, and `PAYLOAD`. Polynomial `0x07`, initial value `0x00`.

## Hardware session gate

1. `StartSession()` requires a non-empty human confirmation token.
2. The session binds a device identity and technology.
3. `WiFiCapabilityAgent.BindValidationSession()` binds incoming hardware results to that session.
4. `CompleteSession()` accepts evidence only when the session ID, device identity and timestamp match.
5. Policy-disabled and documented-not-supported capabilities remain blocked from automatic promotion.

The human token is an application-level confirmation gate, not cryptographic authentication.

## Scope

Wireless Lab is for capability discovery and verification on owned/authorized hardware. Disruptive operations remain policy-gated and are not automatically exposed.
