# Wireless Security Lab

`Wireless Security Lab` is a separate defensive/security-research layer for PM5 Control Center. It must not be mixed into the normal USB, TCP, Wi-Fi or BLE transport implementation.

## Scope

The first implementation is passive analysis of Wi-Fi observations supplied by the host operating system or a future PM5/BWM telemetry adapter.

It can identify:

- duplicate SSIDs with different BSSIDs;
- suspicious signal-strength differences;
- duplicate SSIDs appearing on different channels;
- security-mode inconsistencies;
- candidate rogue-AP / Evil-Twin conditions;
- evidence and confidence without claiming that a duplicate SSID is automatically malicious.

The core analyzer is `PM5Control.Core.WirelessSecurityLab.RogueApAnalyzer`.

## Important boundary

The project does **not** implement an operational Evil-Twin attack, client deauthentication, beacon flooding, credential harvesting, password verification, or forced client migration. Those mechanisms can disrupt third-party networks or capture credentials. They are intentionally outside the normal PM5 Control Center implementation boundary.

A controlled laboratory can still use this module to analyze observations from an AP that the tester owns. A future simulator may generate synthetic duplicate-SSID observations so the detection logic can be tested without transmitting attack traffic.

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
8. A simulator with deterministic synthetic RF observations.
9. A GUI view under a dedicated **Wireless Security Lab** section.
10. Exportable evidence reports without storing credentials.

All findings should retain the project's existing evidence labels (`DETECTED`, `REPORTED`, `EXPECTED`, `HYPOTHESIS`, `SIMULATED`, `UNKNOWN`) and should never turn a heuristic into a confirmed compromise without supporting evidence.

## External research references

The architecture was informed by public ESP32 wireless-security projects demonstrating rogue-AP/Evil-Twin concepts and by defensive Wi-Fi security-lab projects. These references are research inputs only; their offensive packet-injection and credential-capture components are not imported into PM5 Control Center.
