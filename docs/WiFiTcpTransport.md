# PM5 BWM Wi-Fi TCP transport

`WifiTcpTransport` provides a TCP byte-stream boundary for a PM5 BWM endpoint.

## Protocol boundary

The transport delegates framing to `WirelessProtocol`:

```text
[0xAA][CMD][LEN][PAYLOAD...][CRC8-CCITT][0x55]
```

It handles TCP fragmentation, coalesced frames, garbage before SOF, CRC/EOF validation and bounded receive-buffer growth.

## Evidence boundary

This code does not claim that a particular BWM firmware exposes a TCP listener. It also does not invent a PM3/NG-over-TCP packet format or a firmware-flashing command.

The upstream command documentation lists `hw bwm wifi` as bringing up Wi-Fi STA + TCP server for a `tcp:` connection. Runtime/device verification is still required before marking the path hardware-verified.

Firmware update support remains evidence-gated until the upstream flashing-over-Wi-Fi protocol is merged and verified.
