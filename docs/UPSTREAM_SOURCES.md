# Upstream Sources and Firmware Families

## Purpose

This document prevents a common mistake: treating every repository containing `proxmark3` code as if it were a firmware repository for every Proxmark-family device.

The Control Center uses upstream projects as **evidence sources**. A source is not automatically a compatibility recommendation.

## 1. RfidResearchGroup / Iceman Proxmark3

Primary reference:

- https://github.com/RfidResearchGroup/proxmark3

The RfidResearchGroup repository is an Iceman-oriented Proxmark3 software/firmware project. It is a major source of Proxmark3 protocol, client, FPGA/ARM and RFID functionality knowledge.

Important distinction:

- **Iceman** is the name commonly associated with this enhanced Proxmark3 software/firmware lineage.
- **RfidResearchGroup (RRG)** is the GitHub organization maintaining the current repository.
- **Proxmark3** is the hardware family targeted by that repository.
- None of those names alone proves that a particular firmware image is suitable for a Proxmark5.

The upstream README explicitly describes the project as Proxmark3 software and documents Proxmark3 hardware variants such as RDV4. Therefore this repository is treated as a **reference/legacy compatibility source**, not as the PM5 firmware authority.

## 2. Proxmark5 BWM / ESP32

PM5-specific wireless/BWM reference:

- https://github.com/RfidResearchGroup/Proxmark5_BWM_esp32

This repository is specifically described as the BLE/Wi-Fi module based on ESP32-C2 for Proxmark5. Its command/API and firmware information are therefore relevant to the PM5 BWM subsystem.

The Control Center must keep BWM information separate from the main ARM/FPGA firmware identity.

## 3. Proxmark3 versus Proxmark5

The project deliberately treats these as different hardware families unless evidence proves a narrower compatibility relationship.

| Area | Proxmark3 family | Proxmark5 project |
|---|---|---|
| Hardware identity | PM3 variants such as RDV3/RDV4 | PM5 hardware/revisions to be detected |
| Main software reference | RRG/Iceman PM3 tree | PM5-specific sources plus verified compatible components |
| ARM/FPGA | PM3-specific implementation | Must be identified from PM5 hardware/firmware |
| ESP32/BWM | Not the defining PM3 architecture | First-class PM5 subsystem where fitted |
| Firmware selection | PM3-specific candidates | PM5-specific candidates only after identity/compatibility checks |
| Protocol reuse | Potentially reusable components | Must be proven per component |

This table is architectural guidance, not a claim that every listed subsystem differs in every revision.

## 4. What the application should tell the user

Example:

> **Detected:** Proxmark5, revision X
>
> **Reference:** RfidResearchGroup/Iceman Proxmark3 software is available, but it targets the Proxmark3 family. Some protocol/client components may be reusable; that does not make a PM3 firmware image compatible with this PM5.
>
> **BWM:** A separate PM5 BWM/ESP32 firmware component is present and is checked independently.
>
> **Firmware recommendation:** only packages with an established PM5 hardware/revision match are shown as compatible.

For an unknown revision:

> The device family appears to be PM5, but the exact hardware revision is not verified. Firmware selection is therefore **UNKNOWN**, not Compatible.

## 5. Evidence requirements

Every future compatibility entry should record, where available:

- source repository;
- branch/tag;
- exact commit SHA;
- source file/path;
- date checked;
- hardware family;
- exact hardware revision;
- firmware component;
- firmware version/build identifier;
- compatibility status;
- evidence level (`SOURCE_VERIFIED`, `PROTOCOL_VERIFIED`, `HARDWARE_VERIFIED`).

A repository URL by itself is never sufficient evidence for a firmware recommendation.

## 6. Firmware-selection decision tree

```text
USB/device detected
      |
      v
Identify hardware family
      |
      +-- PM3 --> PM3 compatibility branch
      |
      +-- PM5 --> PM5 compatibility branch
      |
      +-- unknown --> stop firmware selection
      |
      v
Identify exact revision
      |
      v
Identify ARM / FPGA / BWM components
      |
      v
Match compatibility registry
      |
      +-- established match --> show candidate + explanation
      |
      +-- mismatch --> explain why it is rejected
      |
      +-- insufficient evidence --> show UNKNOWN and do not recommend
```

## 7. Rule for future AI assistants

Do not solve a compatibility question by selecting the most feature-rich or newest firmware repository. First identify the physical hardware and then prove the compatibility relationship.

When evidence is missing, preserve `UNKNOWN` and add a research question instead of inventing a compatibility claim.
