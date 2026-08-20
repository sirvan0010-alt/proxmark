# Hardware Comparison and Firmware Guidance

## Purpose

The Control Center must help a human understand **why the connected hardware matters** before it recommends firmware or compatibility actions.

The UI should not make the user interpret VID/PID values or repository names by themselves.

## Human-readable comparison

The diagnostic screen should show something similar to:

```text
Connected device
  Family:             Proxmark5
  Hardware revision:  <detected / unknown>
  ARM firmware:       <detected / unknown>
  FPGA:               <detected / unknown>
  ESP32/BWM firmware: <detected / unknown>

Reference comparison
  Proxmark3 family:   legacy/reference profile
  Proxmark5:          separate hardware family

Compatibility
  PM3 protocol reuse: compatible / partial / unknown / incompatible
  Firmware package:   compatible / unknown / incompatible

Why this matters
  Firmware must match the actual hardware revision and subsystem layout.
  A PM3 repository entry is not sufficient evidence that a firmware image is
  suitable for a PM5 device.
```

The exact values remain unknown until detected or sourced from reliable evidence.

## Firmware selection workflow

```text
USB/device detection
        ↓
Hardware family
        ↓
Exact hardware revision
        ↓
Current ARM / FPGA / BWM versions
        ↓
Compatibility database
        ↓
Human-readable explanation
        ↓
Candidate firmware packages
        ↓
Explicit user confirmation
```

The application should explain a mismatch in plain language, for example:

> "The connected board identifies as PM5 revision X, but this firmware package is registered only for PM3 revision Y. It is therefore not offered as a compatible update."

It should also explain an unknown result:

> "The device family was detected, but the exact hardware revision could not be verified. Firmware selection is disabled until the revision is identified."

## PM3 versus PM5 rule

The RfidResearchGroup `proxmark3` project is a valuable upstream/reference source, but the repository name alone does not establish PM5 firmware compatibility.

The client should distinguish:

- **hardware identity** — what the physical device is;
- **protocol compatibility** — which protocol components can communicate;
- **firmware compatibility** — which image is intended for the exact hardware;
- **feature compatibility** — which functions are actually exposed by the connected firmware.

These are separate checks.

## Future firmware browser

When the database contains enough verified entries, the client can show:

| Firmware candidate | Hardware | Current version | Compatibility | Evidence |
|---|---|---|---|---|
| Candidate A | PM5 rev X | older | Compatible | Hardware verified |
| Candidate B | PM3 rev Y | unrelated | Incompatible | Compatibility database |
| Candidate C | PM5 family | unknown revision | Unknown | Missing hardware identity |

No candidate should be presented as safe to flash merely because it exists upstream.

## Why this is useful

The goal is not only to automate firmware selection. It is to make the difference between PM3 and PM5 understandable to a normal user and to make every recommendation traceable to evidence.
