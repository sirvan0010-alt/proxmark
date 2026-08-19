# First Hardware Session Checklist (Read-Only)

**Status:** Ready for use  
**Scope:** First real PM5 hardware baseline  
**Rule:** No firmware write, no configuration change, no BWM network reconfiguration.

This checklist is the operational boundary between software-only development and the first physical Proxmark5 session.

## 0. Pre-session preparation

- [ ] Confirm repository/client commit SHA.
- [ ] Confirm the current upstream research snapshot date.
- [ ] Create a local output directory outside Git: `local-data/session-YYYY-MM-DD/`.
- [ ] Use a known-good USB-C data cable.
- [ ] Connect PM5 directly to the Windows PC for the first session; avoid hubs.
- [ ] Ensure no automatic firmware-update action is enabled.
- [ ] Keep the device in its original state before inspection.

## 1. USB enumeration (Windows)

| Field | Value | Source state | Notes |
|---|---|---|---|
| Device name | | DETECTED / REPORTED | |
| VID | | DETECTED | hex |
| PID | | DETECTED | hex |
| USB class/interface | | DETECTED / REPORTED | |
| Driver provider | | REPORTED | |
| Driver version | | REPORTED | |
| COM/interface path | | DETECTED | if applicable |
| Power/bus state | | REPORTED / UNKNOWN | |

**Stop conditions**

- Expected interface is missing and the required driver is unknown → stop; do not install a replacement driver blindly.
- A tool proposes writing firmware or changing device state → stop.

## 2. Transport identification

- [ ] Identify the transport used by an existing compatible client, if available.
- [ ] Record CDC/serial/USB interface characteristics only when observed or documented.
- [ ] Record whether the device is composite and which interfaces are present.
- [ ] Do not infer a PM5-specific transport solely from PM3 behavior.

## 3. Main PM5 system

| Field | Value | Source state | Confidence | Evidence |
|---|---|---|---|---|
| Hardware model | | | | |
| Hardware revision | | | | |
| Device ID / serial | | | | |
| ARM firmware | | | | |
| ARM build/commit/date | | | | |
| FPGA image/version | | | | |
| Reported memory | | | | |
| RF capability summary | | | | |

Never mark inferred values as `DETECTED`.

## 4. BWM / ESP32 subsystem

| Field | Value | Source state | Confidence | Evidence |
|---|---|---|---|---|
| ESP32/MCU model | | | | |
| BWM firmware version | | | | |
| Build date/time | | | | |
| MAC address | | | | |
| Free heap | | | | |
| Uptime/timestamp | | | | |
| UART/interface settings | | | | |
| Ready state | | | | |
| NVS/system statistics | | | | |

Only record fields actually exposed by the firmware.

## 5. Wireless (read-only)

| Field | Value | Source state | Notes |
|---|---|---|---|
| Wi-Fi support | | | |
| Wi-Fi current state | | | Do not change configuration |
| BLE support | | | |
| BLE current state | | | |
| TCP/UDP capability | | | |
| MQTT capability | | | |

## 6. Power and battery

| Field | Value | Source state | Notes |
|---|---|---|---|
| External power present | | | |
| Battery present | | | |
| Voltage | | | only if exposed |
| Current/charge state | | | only if exposed |
| Charging state | | | |
| Temperature | | | only if exposed |
| Percentage | | | only if a real gauge reports it |

Do not infer battery percentage from voltage unless explicitly documented as an estimate.

## 7. Compatibility comparison

Record separately:

- matches;
- mismatches (show both values);
- unknowns;
- evidence;
- confidence.

Do not overwrite detected values with expected compatibility values.

## 8. Baseline artefacts (local, gitignored)

```text
local-data/session-YYYY-MM-DD/
  README.md
  SESSION_LOG.md
  usb.json
  device.json
  diagnostics.json
  bwm.json
  power.json
  environment.json
  raw-log.txt
```

Do not commit secrets, private keys, credentials or personally identifying network data.

## 9. Session close

- [ ] Disconnect cleanly.
- [ ] No firmware/configuration write was performed.
- [ ] Report path recorded.
- [ ] Unknowns and stop conditions recorded.
- [ ] Evidence source recorded for each important fact.

**Completion:** a reproducible read-only report exists and the device state was not intentionally modified.
