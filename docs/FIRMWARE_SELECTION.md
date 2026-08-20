# Firmware Selection and Human Guidance

**Status:** Design ready; real-device verification pending.

## Purpose

The Control Center must not make the user identify firmware by repository name, VID/PID, or filename alone. It must first identify the physical hardware and its independently observable firmware components, then explain compatibility in plain language.

## Device families

### Proxmark3 / PM3-compatible hardware

The established RfidResearchGroup `proxmark3` ecosystem is an important upstream reference for legacy PM3-compatible hardware and protocol knowledge. A firmware package from this ecosystem is **not automatically a PM5 firmware package**.

### Proxmark5

PM5 is treated as a distinct hardware family. It may share protocol concepts or reusable code with PM3-related projects, while also having PM5-specific hardware and an ESP32/BWM subsystem. Shared code does not prove firmware compatibility.

## Iceman / RfidResearchGroup terminology

The application should explain these names as upstream/reference ecosystems, not as interchangeable hardware identities. When a candidate originates from a PM3-oriented repository, the UI must explicitly say that it is a PM3/reference candidate unless evidence establishes compatibility with the detected PM5 hardware.

## Selection algorithm

```text
1. Detect physical hardware family
2. Detect exact hardware revision where possible
3. Detect ARM / FPGA / BWM firmware identities
4. Determine which compatibility records match
5. Explain every accepted/rejected candidate
6. Show only candidates with established compatibility
7. Mark unknown candidates as UNKNOWN, never as compatible
8. Require explicit confirmation before any write/update operation
```

## Human-readable result example

```text
Device: Proxmark5 (revision: DETECTED)

Current components:
  ARM:      REPORTED — <version>
  FPGA:     REPORTED — <version>
  BWM:      REPORTED — <version>

Reference comparison:
  Proxmark3 / RRG-Iceman firmware: NOT automatically compatible
  PM5 firmware candidate A:        COMPATIBLE — evidence recorded
  PM5 firmware candidate B:        UNKNOWN — insufficient evidence

Recommendation:
  Candidate A is the only firmware currently established for this
  hardware/revision. No update has been performed.
```

The exact values above are illustrative only. They must never be populated as detected values until real evidence exists.

## Evidence requirements

Every compatibility entry should record, where available:

- hardware family
- exact hardware revision
- firmware component
- firmware version/build
- source repository
- branch/tag
- exact source commit
- date checked
- compatibility status
- evidence or rationale
- known limitations

## Hard rule

Never recommend a PM3 firmware package for PM5 solely because the repositories share command definitions, framing, RF functionality, or other code. Compatibility is a hardware-specific claim and must be evidence-backed.
