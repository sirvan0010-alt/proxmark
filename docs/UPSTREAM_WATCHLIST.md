# Proxmark upstream watchlist

Last recorded: 2026-08-31

Reference repositories for future Proxmark checks and updates:

## Core Proxmark5 / Proxmark3

- RfidResearchGroup/proxmark3 — upstream Proxmark3/Iceman project
- nieldk/proxmark3 — NielDK development fork, especially PM5/BWM/BLE work
- nemanjan00/pm5-rdv4-antenna-adapter — PM5/RDV4 antenna-adapter work
- Iceman / Iceman1001 — relevant upstream Proxmark ecosystem activity, tracked primarily through RfidResearchGroup/proxmark3 and its PRs

## Wireless / ESP32 research

- Alexxdal/ESP32WifiPhisher
- SkepticSeptic/ESP32-EvilTwin-Deauther
- aadesh0706/IOT-ESP32-Evil-Twin-WiFi-Hacking-Deauthentication-Captive-Portal
- SameerAlSahab/ESP32-Deauther
- justcallmekoko/ESP32Marauder
- C5Lab/projectZero
- Mr-Blonde/RogueAP
- ArthurBogiano/RoguESP32

These are tracked for defensive wireless-security research, protocol understanding, detection, simulation and isolated lab use. Offensive Wi-Fi attack/deauthentication functionality must remain separated from the normal PM5 transport layer.

## Other references

- nfc-tools/libfreefare — NFC/MIFARE library reference
- smartlockpicking.com/ble_hackme — BLE security-lab reference

## PM5/BWM topics to monitor

- BLE transport / `pm5_ble_bridge.py` and cross-platform Bleak transport
- BWM charger/gauge and battery telemetry
- low-battery warning and over-discharge protection
- configurable charging voltage; 4.1 V is the preferred safer beta default
- Wi-Fi connection status and transport diagnostics
- USB/UART/SSC timeout and recovery fixes
- FeliCa / ISO14443-B timeout workarounds
- PM5 FPGA source/release status
- battery charge/discharge and calibration scripts
- `hw status --ms 1` and related diagnostics

## Update / integration policy

When a Proxmark check/update is requested:

1. Inspect current upstream commits, PRs and relevant files.
2. Record exact repository, branch/commit/PR and date.
3. Separate verified PM5-compatible changes from hypotheses and experiments.
4. Compare with our current repository before applying anything.
5. Prefer small, reviewable beta modules over invasive transport changes.
6. Keep `Wireless Security Lab` isolated from normal PM5 transport and firmware management.
7. Check license/provenance before adapting open-source code.
8. Preserve evidence labels: DETECTED, REPORTED, EXPECTED, HYPOTHESIS, SIMULATED, UNKNOWN.
9. Run CI after integrated changes.

Open-source research is used as upstream/reference knowledge, not as unattributed copying. Adapted functionality must retain required license notices and source attribution.
