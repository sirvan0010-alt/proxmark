# Upstream Protocol Findings

## Purpose

This document records verified observations from the RfidResearchGroup/proxmark3 upstream client that affect the design of PM5 Control Center.

The upstream repository is a moving target. Findings must include the checked revision/date and must not be treated as proof that the same mechanism is implemented identically by every Proxmark5 firmware build.

## Finding 1 — The upstream client already has a reusable device API boundary

Checked source: `client/src/pm3.c` in RfidResearchGroup/proxmark3, current upstream file retrieved during project research.

The source exposes a C API including `pm3_open()`, `pm3_close()`, `pm3_console()` and device-name/current-device accessors. `pm3_open()` initializes the client and calls the existing `OpenProxmark()` / `TestProxmark()` path; `pm3_console()` routes a command through the client's command parser.

Implication for PM5 Control Center:

- The existing client is not merely a terminal program; it contains a reusable internal device API boundary.
- However, using `pm3_console()` as our primary architecture would effectively make the new application a CLI wrapper.
- Therefore our client should use a structured protocol adapter where possible and keep raw command execution as a compatibility/fallback layer.

Source: `client/src/pm3.c`, checked 2026-08-19.

## Finding 2 — The upstream transport layer supports more than a physical serial cable

Checked source: `client/src/uart/uart_posix.c`.

The transport implementation recognizes prefixes for:

- normal serial device paths
- `tcp:`
- `udp:`
- `bt:` (Bluetooth, when the relevant platform support is compiled)
- `socket:` (local Unix socket)

The TCP/UDP implementation parses an address/port, defaults the network port to `18888` when none is supplied, establishes the socket and configures TCP_NODELAY for TCP. The Bluetooth path uses RFCOMM when BlueZ support is available.

Implication:

The transport abstraction in PM5 Control Center is justified. A future client can conceptually expose USB, TCP and Bluetooth as transports rather than forcing all functionality through a single COM-port-only design.

Important limitation:

This is evidence about the upstream client transport implementation. It is NOT evidence that a particular Proxmark5 firmware build exposes or enables all of these transports. The actual PM5/BWM firmware must be inspected and tested.

Source: `client/src/uart/uart_posix.c`, checked 2026-08-19.

## Finding 3 — Protocol and transport should remain separate in our design

The upstream code keeps serial/network transport handling in the UART/transport area while command handling and device APIs live elsewhere. This supports our decision to use:

```text
UI
 -> Application
 -> Inspector / Protocol
 -> Transport
 -> Device
```

rather than putting USB/TCP/BLE implementation directly into GUI buttons.

## Finding 4 — Raw CLI compatibility remains useful, but must be isolated

Because upstream exposes a command-processing path, our application can eventually provide a hidden compatibility bridge for functionality that does not yet have a structured adapter.

Example conceptual design:

```text
Structured Inspector command
        |
        +--> native PM5/BWM protocol adapter
        |
        +--> PM3-compatible protocol adapter
        |
        +--> CLI compatibility adapter (fallback)
```

The user still clicks a GUI action. Raw CLI syntax remains an internal implementation detail and is not required knowledge.

## Next research targets

1. Identify the exact packet/message structures used by the upstream client/device communication layer.
2. Identify device-identification and version/capability commands that can be mapped into structured `DeviceInfo` results.
3. Locate PM5/BWM-specific code in current upstream/fork sources.
4. Identify the ESP32/BWM API and transport boundary.
5. Determine what is genuinely available over Wi-Fi/BLE versus merely compiled into the client.
6. Verify the Windows transport implementation separately from POSIX.
7. Compare these findings with the actual firmware shipped on the user's physical Proxmark5 before implementing hardware-specific behavior.

## Design conclusion

The first implementation should **not** shell out to the Proxmark CLI for everything. It should create a native transport/protocol abstraction and use a command bridge only where necessary. This keeps the client maintainable as PM5 firmware and upstream code evolve.
