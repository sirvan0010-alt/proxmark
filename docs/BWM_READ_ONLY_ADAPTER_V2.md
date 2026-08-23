# BWM Read-Only Adapter v2.1

## Status

`IMPLEMENTED / UNIT TESTS ADDED / CI PENDING`

The adapter is the first-session BWM inspector. It is deliberately narrower than the complete upstream `GET_*` command surface.

## Design

```text
BwmReadOnlyAdapter
        |
        +--> BwmReadOnlyPolicy  (explicit 13-command allow-list)
        |
        +--> BwmFrameCodec      (framing + CRC validation)
        |
        +--> IProxmarkTransport (USB/BLE/Wi-Fi transport boundary)
```

The policy is a separate artifact so safety does not depend on enum naming, reflection, or a convention such as `Get*`.

## Corrected first-session command contract

| ID | Command | Response shape |
|---:|---|---|
| 1000 | `GET_VERSION_INFO` | UTF-8 string |
| 1001 | `GET_DEVICE_MODEL` | exactly 2-byte little-endian `uint16` |
| 1002 | `GET_SYS_FREE_HEAP` | 4-byte little-endian `uint32` |
| 1003 | `GET_SYS_TIMESTAMP` | 4-byte little-endian `uint32` |
| 1004 | `GET_APP_COMPILE_DATETIME` | UTF-8 string |
| 1006 | `GET_SYS_TIME_ZONE` | UTF-8 string |
| 1008 | `GET_SYS_BASE_MAC_ADDR` | exactly 6-byte MAC |
| 1009 | `GET_SYS_UART_CMD_BAUD_RATE` | 4-byte little-endian `uint32` |
| 1010 | `GET_SYS_UART_CMD_MAX_BAUD_RATE` | 4-byte little-endian `uint32` |
| 1012 | `GET_SYS_NVS_STATS` | exactly 20-byte payload, retained raw |
| 1015 | `GET_LOG_UART_FORWARD_ENABLE` | exactly 1-byte status |
| 1017 | `GET_LOG_LEVEL` | exactly 1-byte `uint8` |
| 1018 | `GET_SYS_READY_STATUS` | exactly 1-byte status |

These 13 IDs are pinned to the upstream firmware snapshot `RfidResearchGroup/Proxmark5_BWM_esp32` commit `b918166128e05455c2dcb4e232216d453bbf29ee`.

## Explicit exclusions

The adapter does not send configuration setters, start/stop operations, OTA, reboot, passthrough, Wi-Fi connection/password getters, MQTT credentials, certificates/private keys, PSK material, or BLE bonding secrets.

Some of those commands are technically GET operations, but they are outside the first-session read-only inspector because read-only must not be interpreted as harmless disclosure of secrets or as permission to enter a later network-management workflow.

## Evidence boundary

BWM framing, command IDs and CRC are source-verified. A valid CRC and response frame prove protocol framing, not physical-device identity.

`DiagnosticConfidence.Medium` means that the response shape/type is supported by the pinned protocol source. It does **not** mean that a physical PM5 has returned that value.

Structured payloads whose semantic fields are not independently hardware-verified remain raw or `UNKNOWN`.

## Hardware identity boundary

`BwmIdentityInspector` intentionally keeps the family `UNKNOWN` until independent hardware evidence is available. Seeing BWM responses, including model ID `0xDA10`, is not by itself permission to label the device PM5 or to select a PM5 firmware branch.

The human-readable assessment explicitly distinguishes:

- PM5
- legacy PM3
- Iceman PM3 variant
- RRG PM3 variant
- unknown hardware

The compatibility registry keeps those reference families separate from detected facts. Until exact USB/device revision and subsystem firmware evidence matches a registered entry, the firmware recommendation is `DO_NOT_RECOMMEND_FIRMWARE`.

This prevents the Inspector from recommending PM3/Iceman/RRG firmware for PM5 merely because the projects share protocol code or repositories.

## Simulator boundary

`BwmSimulatedTransport` is `SIMULATED_ONLY`. Its fixtures now cover all 13 source-documented response shapes, including the corrected 2-byte model ID, but simulator output remains test data and is never hardware evidence.

## Tests

The v2.1 test suite covers:

- exact 13-command allowlist;
- deny-by-default for mutating and sensitive commands;
- all 13 response shapes;
- little-endian model ID;
- CRC failure;
- wrong response command ID;
- broadcast instead of response;
- payload-length rejection;
- identity/firmware recommendation safety boundary.

## Async BWM events

`IProxmarkTransport.DataReceived` remains the correct boundary for unsolicited BWM broadcasts. The read-only request path intentionally accepts only a correlated `Response`; broadcast/event routing remains a separate transport/protocol integration step.

## Next blocker

The next step is real-device validation:

1. enumerate the physical PM5 over USB;
2. capture USB VID/PID and device identity evidence;
3. establish the actual BWM transport path and baud;
4. run the 13-command read-only probe sequence;
5. capture exact request/response frames and latency;
6. compare payload lengths and values with the source-backed model;
7. create the first evidence-backed hardware registry entry;
8. only then allow a concrete firmware branch recommendation.

No firmware flashing or configuration mutation belongs in this first physical session.
