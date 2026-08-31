# PM5 BWM Wi-Fi TCP transport

## Protocol boundary

`WifiTcpTransport` is deliberately tied to `PM5Control.Core.WirelessLab.WirelessProtocol`.
It does **not** use the hypothetical PM3/PM5 NG packet layout from `Pm5RfidCommandBuilder`.
The BWM protocol currently present in this repository is:

```text
[0xAA][CMD][LEN][PAYLOAD...][CRC8-CCITT][0x55]
```

The TCP layer treats TCP as a byte stream and therefore buffers input until a complete,
CRC-valid BWM frame is available. It also handles fragmented frames and multiple frames
coalesced into one `ReadAsync` operation.

## Hardware verification boundary

The current ESP32-C2 capability-test firmware supplied for the BWM work initializes UART
and implements the BWM frame parser on UART. It does **not** create a TCP server. Therefore
`WifiTcpTransport("192.168.4.1", 7901)` is an integration component for a future/other BWM
firmware TCP bridge; it is not evidence that port 7901 is currently exposed by the firmware.

Do not label the TCP path `HARDWARE_VERIFIED` until a physical BWM image with a TCP listener
has been tested end-to-end.

## Why the earlier `ITransport` proposal was rejected

The proposed `Pm5RfidCommandBuilder` used a guessed PM3/NG packet format, guessed CRC-16,
and guessed PM5 BWM command IDs. None of those guesses are compatible with the repository's
verified BWM framing layer. Adding that abstraction unchanged would make the project compile
around an unverified wire protocol and would create false-positive compatibility claims.

The existing `WirelessProtocol.BuildFrame` / `TryParseFrame` implementation is the authoritative
BWM framing boundary in this repository.

## Scope

This transport is suitable for benign BWM capability discovery, status, scan and other
commands already defined by `WirelessProtocol`. It does not implement credential guessing,
identity impersonation, or automated attempts to discover a third party's accepted RFID
credential.
