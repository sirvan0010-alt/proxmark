# PM5 BWM Battery / Charger Safety — Evidence Boundary

Status: **SUPERSEDED AS AN AUTOMATIC-CONFIGURATION POLICY**

Date reviewed: 2026-09-13

## Important correction

The earlier project policy treated 4.10 V as a preferred automatic charger target. That was too strong for the current evidence level.

The upstream BWM repository documents the hardware around an ESP32-C2/ESP8684, BQ27427 fuel gauge and AW32001E charger, but source documentation and a successful CI build do not prove that a particular physical PM5+BWM unit exposes a safe charger-register write/readback path through the Control Center.

Therefore the Control Center **must not automatically configure the charger** and must not present 4.10 V as an active hardware setting.

## Current rule

1. No automatic charger-register writes.
2. No guessed battery chemistry.
3. No claim that a particular charge voltage is active unless the physical device reports it and the observation is reproducible.
4. Live battery voltage, SoC, charge state and charger state must remain separate observations.
5. A future charger-control implementation requires an exact upstream protocol/source reference, explicit command classification, safe bounds derived from the actual hardware/pack, and physical read-back testing.

## Evidence ladder

- `SOURCE VERIFIED`: upstream source documents the charger/fuel-gauge components or firmware behaviour.
- `CI VERIFIED`: upstream firmware builds successfully.
- `HARDWARE VERIFIED`: the physical PM5+BWM reports the observation through a verified interface.
- `CHARGE VERIFIED`: a controlled charge cycle has been observed and recorded.

The current project is only at the first two levels for the generic BWM baseline.

## Why this document remains

The document is retained as an audit trail so the earlier 4.10 V policy is not silently forgotten or mistaken for current hardware evidence. The active compatibility record is `compatibility/bwm.json`.
