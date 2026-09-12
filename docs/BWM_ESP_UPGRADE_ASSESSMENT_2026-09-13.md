# BWM ESP Upgrade Assessment — 2026-09-13

## Question

Can the ESP on the current Proxmark5 BWM board simply be replaced with a more powerful ESP?

## Answer

Not as a firmware-only change and not as a safe drop-in assumption.

The upstream BWM hardware is designed around an **ESP32-C2 / ESP8684** and also contains the BQ27427 fuel gauge and AW32001E charger/power-management circuitry. The published schematic is the authoritative source for the actual board-level connections.

A faster/newer ESP can be a valid **next PCB revision**, but a candidate must be checked against the complete hardware boundary before calling it compatible.

## What must match

A replacement MCU would need compatible or redesigned:

- power rails and peak current requirements;
- reset/enable and boot/strapping circuitry;
- UART connection to the PM5 host;
- I2C connection to the fuel gauge/other peripherals;
- GPIO assignments used by the BWM firmware;
- flash/package requirements;
- crystal/clocking and boot support;
- RF matching network and antenna path;
- PCB antenna/layout constraints;
- EMI and coexistence behaviour;
- firmware/ESP-IDF support;
- physical package and PCB footprint.

The fact that two chips are both called ESP32 does **not** make them pin-compatible or RF-compatible.

## Candidate direction

For a future redesigned BWM board, families such as **ESP32-C3** or **ESP32-C6** are technically interesting candidates because they provide more MCU resources and/or newer wireless capabilities than the ESP32-C2. They must be treated as redesign candidates, not drop-in replacements.

The ESP32-C6 is particularly interesting for a new board revision if the project wants newer Wi-Fi/BLE and additional wireless capabilities, but that does not imply compatibility with the current PCB.

## Recommended engineering path

1. Keep the current ESP32-C2/ESP8684 board as the compatibility baseline.
2. Capture the complete SCH.pdf pin/net map into a hardware-baseline record.
3. Select one candidate MCU.
4. Compare every required net and electrical constraint.
5. Redesign the RF section if the candidate requires it.
6. Build a separate BWM prototype PCB rather than modifying production hardware first.
7. Port the upstream BWM firmware and preserve the existing binary protocol where possible.
8. Run the PM5↔BWM physical validation matrix.
9. Only then add a new hardware profile to PM5 Control Center.

## Control Center rule

The Control Center must not report a hypothetical upgraded ESP as supported merely because the firmware could theoretically compile for it. A new MCU becomes a supported hardware profile only after source/firmware evidence and physical electrical/wireless validation are both available.

## Current baseline

- Upstream repository: `RfidResearchGroup/Proxmark5_BWM_esp32`
- Current inspected commit: `e0c4982800eb7d1ca16045631f3460f9f176427c`
- Current documented module MCU: ESP32-C2 / ESP8684
- Current documented UART: 460800 baud
- Current status: source/CI verified; physical PM5+BWM operation still pending

This document intentionally does not claim that C3/C6 is pin-compatible with the existing BWM PCB.
