# Pre-Hardware Final Check

## Goal

Reach the physical PM5 with the software prepared so the first USB connection is an observation session, not a coding session.

## Repository

- [ ] Working tree changes are committed normally.
- [ ] No force-push/reset/squash was used.
- [ ] Current HEAD is recorded after the final pre-hardware change.
- [ ] GitHub Actions workflow is present.

## Protocol

- [ ] BWM frame codec exists.
- [ ] CRC implementation has source-level provenance.
- [ ] CRC scope is documented separately from PM3 assumptions.
- [ ] BWM command IDs have provenance.
- [ ] Read-only adapter uses an explicit allow-list.
- [ ] Mutating BWM commands are rejected before transport.
- [ ] Cancellation is not silently converted to a normal failure.

## Simulator

- [ ] Normal read-only BWM flow is covered.
- [ ] Malformed response is covered.
- [ ] Wrong command ID is covered.
- [ ] Broadcast-as-response is covered.
- [ ] Timeout is covered.
- [ ] Connection loss is covered.
- [ ] Unsupported command is covered.
- [ ] Simulator results are explicitly `SIMULATED`.

## Diagnostics

- [ ] Unknown values remain `UNKNOWN`.
- [ ] Registry expectations never become detected facts.
- [ ] Human-readable report can explain PM5 vs PM3/reference profiles.
- [ ] Firmware recommendations require hardware identity.
- [ ] No fabricated PM5 VID/PID/revision/memory values exist.

## First physical session — do not skip

1. Connect PM5 by a known-good USB data cable.
2. Observe Windows Device Manager enumeration.
3. Record VID/PID/interface/driver information.
4. Do not install or replace drivers automatically.
5. Run read-only inspection only.
6. Record PM5 hardware revision if exposed.
7. Record ARM firmware version/build.
8. Record FPGA version/image if exposed.
9. Record ESP32/BWM version/build and MAC if exposed.
10. Record power/battery telemetry only if actually reported.
11. Export the raw evidence and structured diagnostic report.
12. Compare the observation with the compatibility registry.
13. Only after the identity is established decide whether transport integration or firmware research is required.

## Explicit first-session prohibition

Do **not** flash, update, erase, restore factory settings, change BWM networking or otherwise modify firmware/device configuration during the baseline session.

The first session should establish a trustworthy baseline that later changes can be compared against.

## Stop conditions

Stop and document the issue if:

- USB identity is unexpected;
- a driver change is proposed without evidence;
- the device reports an unexpected firmware family;
- protocol framing does not validate;
- CRC validation fails;
- a supposedly read-only operation requires a write;
- firmware selection remains ambiguous.

Unknown is a valid result. A guessed answer is not.
