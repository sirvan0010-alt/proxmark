# Wireless Security Lab

`Wireless Security Lab` is a separate defensive/security-research layer for PM5 Control Center. It must not be mixed into the normal USB, TCP, Wi-Fi or BLE transport implementation.

## Scope

The beta is focused on passive observation, detection and controlled simulation:

- duplicate SSIDs with different BSSIDs;
- suspicious signal-strength differences;
- duplicate SSIDs appearing on different channels;
- security-mode inconsistencies;
- candidate rogue-AP / Evil-Twin conditions;
- passive deauthentication/disruption detection;
- BLE advertisement anomaly detection;
- evidence and confidence without claiming that a duplicate SSID is automatically malicious;
- deterministic simulator data for repeatable tests.

The core analyzer is `PM5Control.Core.WirelessSecurityLab.RogueApAnalyzer`.

## Important boundary

The project does **not** implement an operational Evil-Twin attack, client deauthentication, beacon flooding, credential harvesting, password verification, forced client migration, or traffic interception. Those mechanisms can disrupt third-party networks or capture credentials and are intentionally outside the normal PM5 Control Center implementation boundary.

A controlled laboratory can still use this module to analyze observations from an AP that the tester owns. A future simulator may generate synthetic duplicate-SSID observations so detection logic can be tested without transmitting attack traffic.

## Why the stronger-signal scenario matters

A rogue AP can advertise the same SSID as a legitimate AP, and client roaming/selection behaviour can make signal strength relevant. However, signal strength alone does not prove that a client will associate with the rogue AP: security parameters, BSSID handling, roaming policy, PMF, saved-network state and the client OS all matter.

Therefore the detector treats a stronger duplicate as **evidence**, not as proof of compromise.

## Planned extensions

1. OS-level passive Wi-Fi scan adapters for Windows/Linux/macOS.
2. BSSID history and first-seen/last-seen tracking.
3. Baseline profiles for trusted APs.
4. WPA2/WPA3/PMF consistency checks.
5. Rogue-AP confidence scoring.
6. Passive deauthentication/disruption detection.
7. BLE advertisement anomaly detection.
8. Deterministic wireless-security simulator.
9. GUI view under a dedicated **Wireless Security Lab** section.
10. Exportable evidence reports without storing credentials.

## External research references

Tracked in `docs/UPSTREAM_WATCHLIST.md`, including public ESP32 wireless-security projects, `nfc-tools/libfreefare`, and the BLE HackMe reference. These are research inputs only. Their offensive packet-injection and credential-capture components are not imported into PM5 Control Center.

## Evidence model

Every wireless observation should retain:

- source/interface;
- timestamp;
- raw value where available;
- interpreted value;
- confidence;
- `DETECTED`, `REPORTED`, `EXPECTED`, `HYPOTHESIS`, `SIMULATED` or `UNKNOWN`.

A heuristic must never silently become a confirmed compromise.
