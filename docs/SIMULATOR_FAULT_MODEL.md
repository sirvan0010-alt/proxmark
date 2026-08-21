# Simulator Fault Model

## Purpose

The simulator must exercise the client's error handling without pretending that a particular failure sequence has been observed on physical PM5 hardware.

All faults in this document are `SIMULATED` test fixtures.

## Available deterministic faults

| Fault | Simulated condition | Expected client result |
|---|---|---|
| `None` | Normal response | Decode successful response |
| `Timeout` | Transport timeout | No usable diagnostic value; timeout remains distinguishable at transport level |
| `MalformedResponse` | Invalid bytes | Frame rejected |
| `WrongCommandId` | Valid frame for a different command | Response rejected |
| `BroadcastInsteadOfResponse` | Valid broadcast frame where a direct response is expected | Response rejected |
| `DisconnectBeforeSend` | Connection disappears before request | Transport failure; diagnostic remains unknown |
| `UnsupportedCommand` | Device does not provide a response for the command | Diagnostic remains unknown |

## What this proves

These tests prove properties of the **client implementation**:

- malformed frames do not become trusted values;
- responses for the wrong command are not silently accepted;
- broadcasts are not mistaken for direct responses;
- cancellation remains observable;
- transport failures do not create fabricated diagnostic values.

They do **not** prove that a real PM5 produces these exact failures.

## Hardware verification later

When physical PM5 is connected, observed failures should be recorded separately in `docs/PROTOCOL_NOTES.md` with:

- timestamp;
- exact hardware/firmware identity;
- transport;
- request;
- response/error;
- evidence source;
- confidence;
- repository commit.

A hardware observation may then replace or refine a simulator assumption, but the simulator must never be used to retroactively manufacture hardware evidence.
