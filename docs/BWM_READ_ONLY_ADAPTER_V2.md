# BWM Read-Only Adapter v2

## Status

`IMPLEMENTED / STATIC ANALYSIS + UNIT TESTS PENDING CI`

The adapter is the first-session BWM inspector. It is deliberately narrower than the complete upstream `GET_*` command surface.

## Design

```text
BwmReadOnlyAdapter
        |
        +--> BwmReadOnlyPolicy  (explicit allow-list)
        |
        +--> BwmFrameCodec      (framing + CRC validation)
        |
        +--> IProxmarkTransport
```

The policy is a separate artifact so safety does not depend on enum naming, reflection, or a convention such as `Get*`.

## Allowed first-session commands

| ID | Command | Decode |
|---:|---|---|
| 1000 | `GET_VERSION_INFO` | UTF-8 string |
| 1001 | `GET_DEVICE_MODEL` | raw string API + explicit `uint16` model-ID API |
| 1002 | `GET_SYS_FREE_HEAP` | little-endian `uint32` |
| 1003 | `GET_SYS_TIMESTAMP` | little-endian `uint32` |
| 1004 | `GET_APP_COMPILE_DATETIME` | UTF-8 string |
| 1006 | `GET_SYS_TIME_ZONE` | UTF-8 string |
| 1008 | `GET_SYS_BASE_MAC_ADDR` | 6/8-byte MAC |
| 1009 | `GET_SYS_UART_CMD_BAUD_RATE` | little-endian `uint32` |
| 1010 | `GET_SYS_UART_CMD_MAX_BAUD_RATE` | little-endian `uint32` |
| 1012 | `GET_SYS_NVS_STATS` | exact 20-byte payload retained raw |
| 1015 | `GET_LOG_UART_FORWARD_ENABLE` | one-byte boolean |
| 1017 | `GET_LOG_LEVEL` | one-byte value |
| 1018 | `GET_SYS_READY_STATUS` | one-byte boolean |

## Explicit exclusions

The adapter does not send configuration setters, start/stop operations, OTA, reboot, passthrough, Wi-Fi connection/password getters, MQTT credentials, certificates/private keys, PSK material, or BLE bonding secrets.

Some of those commands are technically GET operations, but they are outside the first-session read-only inspector because read-only must not be interpreted as harmless disclosure of secrets or as permission to enter a later network-management workflow.

## Evidence boundary

The BWM framing, command IDs and CRC are source-verified against the upstream firmware snapshot recorded in `docs/BWM_PROTOCOL.md` and `docs/BWM_COMMAND_REFERENCE.md`.

Payload decoding is intentionally conservative. Where a structured payload's exact semantic fields are not yet hardware-verified, the adapter either exposes raw bytes or reports `UNKNOWN` rather than inventing field meanings.

`DiagnosticConfidence.Medium` therefore means the response shape/type is supported by the documented protocol source, not that a physical PM5 has returned that value yet.

## Next blocker

The next step after CI is real-device validation:

1. enumerate the physical PM5 over USB;
2. establish the actual BWM transport path and baud;
3. run the 1001/1000/1018/... read-only probe sequence;
4. capture exact request/response frames and latency;
5. compare payload lengths and values with the source-backed model;
6. promote only directly observed facts to `HARDWARE_VERIFIED`.

No firmware flashing or configuration mutation belongs in this first session.
