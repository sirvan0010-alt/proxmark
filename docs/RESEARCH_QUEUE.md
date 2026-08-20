# PM5 Research Queue

This queue contains questions that cannot be answered reliably from the current repository state alone.

## RQ-001 — Identify exact PM5 hardware revision

Status: `UNKNOWN`

Required evidence: direct USB/device inspection.

## RQ-002 — Identify exact ARM firmware

Status: `UNKNOWN`

Required evidence: read-only firmware/device information from the physical PM5.

## RQ-003 — Identify exact FPGA image/version

Status: `UNKNOWN`

Required evidence: physical device or verified PM5-compatible information source.

## RQ-004 — Identify ESP32/BWM firmware

Status: `UNKNOWN`

Required evidence: BWM diagnostic response or documented PM5-specific source.

## RQ-005 — Determine transport matrix

Status: `UNKNOWN`

Question: Which functions are actually available over USB, Wi-Fi/TCP and BLE on the physical PM5?

## RQ-006 — Determine power/battery telemetry

Status: `UNKNOWN`

Question: Which voltage/current/charging/temperature/percentage values are exposed by firmware or hardware?

## RQ-007 — PM3 compatibility boundary

Status: `IN_PROGRESS`

Question: Which PM3/RRG protocol components are reusable on PM5 and which are PM5/BWM-specific?

Required evidence: exact upstream revision plus PM5 observation.

## RQ-008 — Firmware backup mechanism

Status: `UNKNOWN`

Question: What is the verified PM5-compatible backup/extraction path for each subsystem?

Do not implement a guessed dump mechanism.

## Workflow

```text
Question → evidence → implementation/model → test → compatibility entry → documentation
```
