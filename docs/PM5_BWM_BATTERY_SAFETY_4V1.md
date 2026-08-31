# PM5 BWM Battery Safety Profile — 4.10 V

Status: **IMPLEMENTED IN THE PROJECT SAFETY/COMPATIBILITY LAYER**

Date: 2026-08-31

## Decision

The project adopts **4.10 V as the target maximum BWM charge voltage** for PM5 battery-management work, rather than the charger IC's 4.20 V power-on default.

This is a firmware policy choice for the PM5 BWM integration. It is intentionally below the AW32001E's higher hardware capability and is not treated as proof that every PM5 battery pack has the same chemistry or rating.

## Upstream basis

The current upstream/Iceman fork contains BWM charger support for the AW32001E and BQ27427 and exposes the charger voltage register. The upstream code currently documents a firmware safety ceiling of 4.20 V. The Discord discussion supplied for this project reports a proposed 4.10 V configurable default and a PR for it.

Our project therefore tracks 4.10 V as the desired PM5 safety profile, while keeping the exact upstream implementation commit separately recorded during each update audit.

## Required implementation behaviour

1. On BWM initialization, configure the charger target to **4100 mV**.
2. Treat 4100 mV as the default safe target, not as a measured battery voltage.
3. Never infer battery chemistry from voltage alone.
4. Report the configured charge target separately from the live measured battery voltage.
5. If the charger does not acknowledge the configuration register, report the configuration as `UNKNOWN` rather than claiming 4100 mV is active.
6. A user-configurable lower target may be supported later; values above the project safety ceiling require an explicit engineering review.
7. Do not silently raise the target after reboot.
8. Preserve telemetry for configured target, actual battery voltage, charge state and charger fault state independently.

## Why 4.10 V

The supplied engineering discussion identified the PM5 charger default as 4.20 V and proposed reducing it to 4.10 V as a battery-longevity/safety measure. This project follows that conservative direction for its beta BWM profile.

The value is **not** presented as a universal Li-ion charging rule. The actual cell/pack specification remains the authority for a production battery profile.

## Validation ladder

- `STATIC ANALYSIS`: charger register and policy reviewed.
- `UNIT TESTED`: policy/validation logic can be tested without hardware.
- `HARDWARE VERIFIED`: only after reading back the charger register on a real PM5 BWM.
- `CHARGE VERIFIED`: only after observing a complete controlled charge cycle.

Until hardware read-back exists, the UI must label the 4.10 V setting as `EXPECTED`/`CONFIGURED`, not `HARDWARE VERIFIED`.

## Low-battery protection

The related BWM safety work also tracks:

- low-battery warning;
- sustained low-voltage debounce;
- automatic shutdown;
- USB-powered exception handling;
- false-shutdown protection when fuel-gauge SoC calibration is inaccurate.

The shutdown path must use sustained evidence rather than a single noisy SoC sample. Voltage and SoC must be displayed as independent telemetry.

## Out of scope for this profile

This document does not implement or authorize Wi-Fi credential collection, deauthentication, rogue-AP credential theft, or other active wireless attacks. The Wireless Security Lab remains isolated from the normal PM5 transport and battery-management layers.
