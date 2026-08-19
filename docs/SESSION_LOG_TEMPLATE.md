# Session Log Template

Copy this template to `local-data/session-YYYY-MM-DD/SESSION_LOG.md` for a physical PM5 session.

## Metadata

| Field | Value |
|---|---|
| Date (local) | |
| Operator | |
| Repository commit | |
| Client/tool versions | |
| Upstream research snapshot | |
| Host OS | |
| Host notes | |

## Goal

- [ ] First read-only baseline
- [ ] Repeatability check
- [ ] Transport comparison
- [ ] Other:

## Pre-flight

- [ ] Read-only intent confirmed
- [ ] No automatic firmware update
- [ ] Known-good USB-C data cable
- [ ] Direct PC connection or documented hub

## Observed results

### USB
- VID/PID:
- Driver:
- Interface/COM:
- Source/confidence:

### Main PM5
- Model/revision:
- ARM firmware:
- FPGA:
- Memory:
- Source/confidence:

### BWM / ESP32
- Model:
- BWM version:
- MAC:
- Heap/uptime:
- Source/confidence:

### Wireless
- Wi-Fi:
- BLE:
- TCP/UDP:

### Power
- External power:
- Battery:
- Voltage/current/charge:

## Mismatches and unknowns

## Actions performed

1.
2.
3.

## Actions not performed

- Firmware flash
- Configuration write
- BWM network reconfiguration

## Artefacts

| Path | Description | Secrets? |
|---|---|---|
| | | No / Yes |

## Issues / stop conditions

## Next step

## Completion

- Device state modified: **No / Yes** (must be No for the first baseline)
- Report reproducible: **Yes / Partial / No**
- Ready to update hardware verification status: **Yes / No**
