# M1 Implementation Notes — PM5 Inspector Core

M1 is still software-only until the first real PM5 hardware session. No document in this phase may claim verified PM5 behavior without evidence.

## Current foundation

- DiagnosticValue model
- ProxmarkDeviceInfo
- IProxmarkTransport with asynchronous data-event support
- IProxmarkProtocol
- BWM framing: CRC, codec, stream parser, event dispatcher
- Unit tests for BWM framing/parser/event dispatch
- CI pipeline for Linux and Windows

## Implementation order

1. Keep the shared Core authoritative.
2. Finish the thin Ubuntu CLI shell over Core.
3. Keep the first hardware session read-only.
4. After real-device evidence is available, implement the concrete USB transport adapter.
5. Implement the BWM read-only adapter against verified PM5/BWM behavior.
6. Implement PM5 identification/diagnostics orchestration.
7. Add structured JSON/Markdown report export from observed data.
8. Add compatibility definitions only for values supported by evidence.
9. Build the Windows GUI on the same Core.
10. Build the Android client on the same Core/protocol abstractions where platform constraints permit.

## Non-goals before hardware validation

- Firmware flashing
- Guessed memory dumps
- Silent driver installation
- Full RRG feature-parity claims
- Treating PM3 behavior as automatically PM5-compatible
- Public BWM TCP exposure
- Inventing battery telemetry

## M1 exit criteria

- Core builds in CI.
- Relevant unit tests pass in CI.
- Ubuntu CLI exists as a thin shared-Core client.
- First physical PM5 session has a reproducible read-only baseline.
- Every important PM5-specific claim has an evidence source and confidence.
- No hardware behavior is marked verified before the physical test.
