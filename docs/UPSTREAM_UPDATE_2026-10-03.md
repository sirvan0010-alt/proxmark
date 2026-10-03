# PM5 upstream integration snapshot — 2026-10-03

## Repositories and revisions

- RRG proxmark3: TCP default-port change 133b981512f317c38b836f208e9c39afd75b5f28 (18888), follow-ups f9bd1c30105688ad4ae41613d490110e419aa602 and 63063bd030ae0e9cd196068bdf884d4225577fb1; CEP fixes f1cb4952086861f2e27c89fddf0d274269cff9d6 and e6d7cd1f9d330b930073f32cda06e308774cd36d.
- BWM ESP32: PR #8 merge 8153c26efee3ba2bb8dd6485223ac0742e4b165f (2026-10-02), fixes mDNS lifecycle on Wi-Fi disable/restart. Previous mDNS introduction baseline: b450b1336dfe00fb507efb535ff3d8a1d9d036a9.

## TCP default port

The BWM TCP server default changed from 7777 to **18888**. Control Center Core WifiTcpTransport now uses DefaultPort = 18888. Explicit host/port entry remains supported. This is a source-derived default; an installed BWM configuration may override it and should be read from device status where possible.

## mDNS lifecycle

BWM mDNS remains optional (CONFIG_PM5_MDNS_ENABLE). PR #8 ensures mDNS is stopped when Wi-Fi modes are disabled. Control Center's existing DNS-SD parser/UI remains optional and manual host/port remains the fallback. Expected lifecycle: Wi-Fi stop → service disappears; Wi-Fi restart → service is re-announced if enabled. Discovery must tolerate this transition and not classify no-result as device absence.

## CEP

- f1cb4952086861f2e27c89fddf0d274269cff9d6: protect I2C/tick timer use and re-enable CEP by default.
- e6d7cd1f9d330b930073f32cda06e308774cd36d: move memory clearing after time-critical SPI reads to prevent reply corruption.

CEP remains PM5↔Flipper Type-C extended-port communication, not a direct Windows PC transport. These commits are upstream source evidence, not a physical test of our PM5.

## Control Center delta

- Implemented: TCP default 18888 and updated BWM compatibility provenance.
- Already implemented: optional _proxmark5._tcp.local DNS-SD discovery, explicit result selection, manual fallback.
- Next: port default/override tests; mDNS stop/restart rediscovery tests; BLE pairing/passkey/bond/TX-power command audit; bootrom Device Info capability parser and separate OLD-frame wireless-flash design.

## Evidence status

Source/commit review: complete for items above. Code: TCP default changed. Fresh CI: pending. Physical PM5/BWM verification: not performed. No firmware writes were executed.

Research date: 2026-10-03.
