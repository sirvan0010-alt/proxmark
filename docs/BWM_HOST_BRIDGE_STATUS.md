# BWM host transport status

## Current finding

The BWM ESP32-C2 firmware exposes a binary UART protocol. The reference firmware documents its frame format, CRC16-CCITT and read-only command IDs, including the 13-command first-session allow-list used by this project.

However, the PM5 host does **not** expose the BWM UART directly on the normal USB serial connection merely because a COM port exists.

The current RfidResearchGroup/proxmark3 Proxmark5 developer documentation explicitly states that the communication driver between `BWM` and `PM5_ARM` is still a TODO and that operating PM5 via BWM is therefore not supported at that point. It also states that the ARM↔BWM link is protocol-based rather than transparent.

## Consequence for PM5 Control Center

The Windows application must not send BWM binary frames directly to the normal PM5 USB COM port unless that endpoint has independently been proven to be the BWM UART.

A successful `COM3` open proves only that Windows exposed a serial endpoint. It does not prove:

- PM5 hardware family;
- PM5 firmware version;
- presence of the BWM board;
- access to the BWM UART;
- existence of an ARM↔BWM bridge;
- support for BWM command IDs on the PM5 host firmware.

The application therefore now reports the transport limitation instead of repeatedly timing out on direct BWM commands.

## Safe transport states

| Path | Read-only BWM commands | Status |
|---|---:|---|
| Unknown COM endpoint | No | Not authorized |
| Direct, independently verified BWM UART | Yes | Protocol adapter ready |
| Normal PM5 USB → PM5 ARM → BWM bridge | No, until bridge protocol/driver is verified | Blocked by missing upstream driver evidence |

## Next engineering milestone

The next substantial implementation step is **not** another BWM GET command. It is the PM5 ARM↔BWM bridge layer.

That layer must be based on an actual PM5 ARM firmware implementation or an independently verified PM5 host-side bridge protocol. No bridge command ID, framing or routing rule is to be invented.

Once that evidence exists, the already-tested BWM read-only adapter can be attached behind the bridge without changing its 13-command safety allow-list.

## Safety invariant

The following remain prohibited automatically:

- `SET_*`
- `START_*`
- `STOP_*`
- `OTA_BEGIN`
- `OTA_WRITE`
- `OTA_END`
- `REBOOT`
- `RESTORE_TO_FACTORY_SETTINGS`
- `SEND_FORWARD_DATA`

The distinction between `PROTOCOL VERIFIED` and `HARDWARE VERIFIED` also remains mandatory.

## Sources

- `RfidResearchGroup/Proxmark5_BWM_esp32`, commit `b918166128e05455c2dcb4e232216d453bbf29ee`
- `RfidResearchGroup/proxmark3`, Proxmark5 developer documentation at commit `9a083e6873a198ea9e9efed49fd670fdf5fefb02`
