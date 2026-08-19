# BWM Protocol Adapter Requirements

## Scope

This document defines what the PM5 Control Center needs from the Proxmark5 BWM/ESP32 protocol adapter. It is intentionally based on verified upstream material and must be revised when the upstream protocol changes.

## Transport

The current upstream BWM documentation describes communication between the PM5 host and BWM over UART at 460800 baud.

Future external transports can carry the same logical protocol when the BWM firmware exposes a transparent forwarding path.

## Frame classes

The current documented protocol has distinct frame markers for:

- request
- response
- asynchronous broadcast/event

The codec must inspect framing before interpreting command payloads.

## Integrity

The documented CRC is CRC16-CCITT using polynomial `0x1021` and initial value `0xFFFF`.

The implementation must provide deterministic unit tests and reject invalid CRC frames.

## Message handling

The protocol engine must support:

1. multiple frames in one read;
2. fragmented frames across reads;
3. invalid frames;
4. timeouts;
5. response correlation;
6. unsolicited broadcasts;
7. events arriving while a request is pending;
8. clean disconnect/reconnect.

## Read-only Inspector commands

The initial adapter should prioritize documented read-only information such as:

- version information
- device model
- system free heap
- system timestamp
- application compile date/time
- base MAC address
- UART command baud information
- NVS statistics
- system ready status
- Wi-Fi status/configuration information where read-only commands exist
- BLE status where exposed

Exact command IDs must be generated from the verified upstream definition used for the implementation and must be associated with an upstream revision/date.

## Capability discovery

Do not infer support from the presence of a source file alone. A feature should be reported as:

- supported and verified;
- reported by firmware;
- known but not verified on this device;
- unsupported;
- unknown.

## Wi-Fi scanner

The scanner is an asynchronous operation. The adapter must model scan results as events rather than assuming that the start command itself contains all results.

A future GUI may display:

```text
SSID | BSSID | RSSI | Channel | Security | WPS
```

The scanner is a diagnostic/network capability and should not be mixed into unrelated RFID workflow code.

## Network forwarding

BWM can expose network transport functions. The client must distinguish:

- local PM5↔BWM UART transport;
- BWM network forwarding;
- remote client connection.

A future remote client should not assume that a public TCP listener is safe. Private LAN/VPN operation is preferred.

## Battery/power

The BWM hardware documentation identifies a fuel-gauge/charger subsystem. The client must only display telemetry that the connected firmware actually exposes.

Hardware component presence does not automatically mean that every telemetry field is readable through the current protocol.

## Versioning

Every adapter build should retain:

- upstream repository
- commit hash
- research date
- protocol revision if known

If the upstream command table changes, the compatibility layer should allow version-specific definitions rather than silently reusing incompatible command IDs.
