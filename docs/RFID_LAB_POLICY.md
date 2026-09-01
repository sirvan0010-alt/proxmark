# RFID Lab Policy (PM5 Control Center)

## Scope

RFID work in this repository is limited to **authorized laboratory use**:

- own / explicitly authorized test cards and readers
- research, inventory, and diagnostic sessions
- no claims of physical access grant from RF-only observations

## Diagnostic truth model

| Observation | Allowed interpretation |
|-------------|------------------------|
| UID seen / anticollision continued | Format accepted at RF layer |
| Auth command observed in trace | Higher-layer challenge may have started |
| Nested/autopwn produced keys in client output | Keys recovered **for that card session** (lab evidence) |
| Door/relay opened | **Not** inferred from RF alone — needs separate channel |

Never promote `AnticollisionSeen` or a byte-scan hit to "access granted".

## Byte-scan ban for production decisions

Scanning a raw buffer for `0x60` / `0x61` / `0x93` / `0x95` / `0x97` **anywhere** is unreliable (UID/CRC/payload collisions). Prefer structured parsers when available; until then label results as low-confidence / legacy.

## Two different parsers (do not mix)

1. **NG UART/USB frames** — host ↔ Proxmark device (`Pm3NgFrame`, magic PM3a/PM3b).
2. **ISO14443-A RF frames** — reader ↔ card over the air.

A correct NG parser does **not** replace an RF frame parser, and vice versa.

## CLI vs binary device API

- Text commands (`hf mf autopwn\n`) are handled by the **Iceman/Proxmark client**, which translates them to binary NG commands.
- Writing ASCII lines to the device COM port only works if a text bridge/client is in the path.
- This repo’s `Connections` / `Protocols/Pm3` path is primarily **binary**. Do not invent fake NG command IDs for nested/hardnested.

## Policy gates

Same spirit as WirelessLab:

- session-gated destructive or long-running ops
- explicit evidence log (command, firmware version, timestamp, raw output)
- UI must not present speculative success as hard fact
