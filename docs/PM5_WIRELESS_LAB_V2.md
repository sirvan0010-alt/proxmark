# PM5 Wireless Lab v2 — corrected integration

The supplied ESP8684 firmware was **not** safe to integrate unchanged.

## Critical defects corrected

1. **Protocol mismatch:** the supplied firmware used XOR CRC, while the current C# `WirelessProtocol` uses CRC-8/CCITT polynomial `0x07`. The firmware now uses the same CRC-8/CCITT algorithm.
2. **False verification:** a successful `esp_wifi_80211_tx()` call only proves that the local API accepted the request. It does not prove that an external receiver observed the RF frame.
3. **Unsafe active-frame scope:** deauthentication/disassociation transmission is not implemented by this probe. Those IDs return an explicit non-success result and remain policy-gated in the host capability matrix.
4. **Sniffer transport bug:** the original callback assembled a buffer but never sent it, and Wi-Fi driver callback context must not be used for arbitrary UART work. The callback is therefore intentionally transport-neutral in this revision.
5. **SoftAP payload bug:** the original `strlen()` could read past the 32-byte SSID field. The corrected implementation uses a bounded length.
6. **Missing command behavior:** `CMD_SET_POWER_MODE` is explicitly rejected instead of being silently ignored.

## Current host-side model

The repository already has session-gated evidence handling. A raw test result first becomes `Tested`; only a matching hardware session may promote it to `HardwareVerified`/`HardwareContradicted`. Documented `NotSupported` capabilities are never silently promoted.

## ESP32-C2 capability basis

ESP32-C2 documentation lists STA, AP, AP+STA, scanning and promiscuous operation. Espressif also documents `esp_wifi_80211_tx()` for beacon, probe-request, probe-response and action-frame classes. This project deliberately keeps the active transmission surface narrower and policy-controlled.

## Build

Build the firmware with the ESP-IDF toolchain for target `esp32c2` from `firmware/ESP8684_Firmware`.

The firmware UART framing must remain byte-for-byte compatible with `src/PM5Control.Core/WirelessLab/WirelessProtocol.cs`.
