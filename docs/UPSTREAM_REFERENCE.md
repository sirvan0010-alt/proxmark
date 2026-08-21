# Upstream and Firmware Reference Map

## Purpose

This document explains which Proxmark repositories are references for the Control Center and, critically, which are **not interchangeable firmware targets**.

The application should present this information in human-readable terms so a user does not have to infer compatibility from repository names, VID/PID values or hexadecimal identifiers.

## 1. Proxmark3 upstream / historical reference

**Repository:** https://github.com/Proxmark/proxmark3

The original `Proxmark/proxmark3` repository is the historical/official Proxmark3 project reference. Its GitHub page currently states that the repository is archived and that active development moved to the RfidResearchGroup/Iceman fork.

Use it for historical context and original PM3 architecture. Do **not** use its existence as evidence that a PM3 firmware image is suitable for PM5.

## 2. RfidResearchGroup / Iceman Proxmark3 repository

**Repository:** https://github.com/RfidResearchGroup/proxmark3

RfidResearchGroup maintains the widely used Iceman fork of the Proxmark3 software/firmware ecosystem. The repository is primarily a PM3-family reference and development source.

It is valuable to this project for:

- PM3 protocol and command references;
- reusable client/protocol concepts;
- source-level comparison;
- identifying which behaviour is PM3-specific;
- tracing historical firmware changes.

It is **not automatically a PM5 firmware repository**.

## 3. Proxmark5 BWM / ESP32 repository

**Repository:** https://github.com/RfidResearchGroup/Proxmark5_BWM_esp32

This repository is specifically associated with the BLE/Wi-Fi module based on ESP32 for Proxmark5. It is therefore a much more direct PM5/BWM reference than the general PM3 repository when investigating the BWM subsystem.

For this project, BWM protocol claims should prefer exact source references from this repository when the claim concerns PM5's ESP32/BWM subsystem.

## 4. What "Iceman" means here

"Iceman" is the name associated with the RfidResearchGroup Proxmark3 fork and its development lineage. It should not be interpreted as a separate hardware family.

In the application, show it as:

```text
RfidResearchGroup / Iceman
    ↓
Proxmark3 software/firmware ecosystem
    ↓
Reference for PM3-compatible behaviour
    ↓
NOT proof of PM5 firmware compatibility
```

## 5. PM3 vs PM5 — user-facing explanation

When a physical PM5 is detected, the application should explain the distinction approximately like this:

```text
Your device: Proxmark5

This is not a generic Proxmark3 board. Some protocol and software concepts
are shared with the Proxmark3 ecosystem, but hardware, firmware and
subsystems must be evaluated independently.

RfidResearchGroup/Iceman:
  Useful reference for Proxmark3-family software and protocol behaviour.

Proxmark5 BWM/ESP32:
  Direct reference for the PM5 BLE/Wi-Fi subsystem.

Firmware recommendation:
  Only firmware registered for this exact hardware family/revision is shown
  as a compatible candidate. Unknown hardware identity disables confident
  firmware selection.
```

## 6. Evidence policy

Every upstream-derived fact should record, where practical:

- repository;
- branch/tag;
- exact commit;
- source file/path;
- date checked;
- whether the claim is PM3 reference, PM5 source-level evidence, simulated, or hardware verified.

A PM3 source observation must never silently become a `DETECTED` PM5 fact.

## 7. Firmware routing rule

The future firmware browser must use this pipeline:

```text
USB/device identity
  → hardware family
  → exact hardware revision
  → current ARM/FPGA/BWM versions
  → compatibility registry
  → evidence-backed candidates
  → human-readable explanation
  → explicit confirmation before any write/update operation
```

If any required identity component is unknown, the result must be `UNKNOWN`, not an optimistic match.

## 8. Current state

The repository deliberately contains no fabricated PM5 VID/PID, revision or firmware entries. The first physical PM5 session is expected to populate those values from evidence.
