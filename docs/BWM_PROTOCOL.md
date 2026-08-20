# BWM Protocol Adapter Requirements

## Verified provenance (2026-08-19, re-confirmed 2026-08-20)

The frame format and command table are verified against the **official**
firmware source, not just documented as "current upstream documentation":

- Repository: `RfidResearchGroup/Proxmark5_BWM_esp32`
- Commit: `b918166128e05455c2dcb4e232216d453bbf29ee` (2026-08-08)
- Files: `components/app_uart_cmd/app_cmd_uart.{c,h}` (wire format, CRC),
  `main/app_com_defs.h` (command codes)

Confirmed against `BwmProtocolConstants.cs`: magic values, CRC polynomial,
CRC initial value, header size, and default UART baud rate all match the
firmware exactly.

**CRC scope, confirmed from source (not a PM3/NG-protocol assumption):**
the firmware's `uart_build_and_send()` computes CRC over
`crc16_ccitt(pkt_buf, idx, CRC16_INIT)` where `idx` at that point already
includes the 2-byte magic header — i.e. CRC covers
`magic + commandId + length + payload`, not just `commandId + length +
payload`. `BwmFrameCodec` in `BwmFrame.cs` must match this scope. This
value was briefly reverted to the wrong (header-excluded) scope during a
repository history rewrite on 2026-08-20 and has been re-applied — see the
Repository Integrity Rule in `AI_CONTEXT.md`, added as a direct result of
that incident.

Command codes are generated 1:1 from `app_com_defs.h` into
`BwmCommandCode.cs` / `BwmBroadcastType.cs` — see the provenance comment at
the top of that file for the exact commit this was generated from. If the
upstream header changes, regenerate rather than hand-editing.

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

Exact command IDs are generated from the verified upstream definition — see `BwmCommandCode.cs` / `BwmBroadcastType.cs` and the "Verified provenance" section above for the associated upstream commit/date.

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
