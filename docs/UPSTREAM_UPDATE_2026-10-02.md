# PM5 upstream integration snapshot — 2026-10-02

## Sources and exact revisions

- `RfidResearchGroup/proxmark3`: latest inspected commit `256f30f0fa7cb2fe84588f3d7ceb5eb3571a3363`.
- `RfidResearchGroup/Proxmark5_BWM_esp32`: latest inspected commit `b450b1336dfe00fb507efb535ff3d8a1d9d036a9`, merge of PR #7 (mDNS support).
- Prior BWM reliability baseline: `4818511a2b179c61f80f54b5f825428cba51deb8` (BLE bulk-transfer retry work).
- PM5 Wi-Fi BWM OTA support: PR #3650 / merge `2630310336c28fd04b4b11aea8e77a7895a55de9`.

## Capabilities schema

Current upstream `include/pm3_cmd.h` declares `CAPABILITIES_VERSION 13`:
- v9: `max_cmd_data_size`
- v11: `em_size`, `em_allocated`
- v12: `compiled_with_bwm`
- v13: `compiled_with_cep`

The Control Center decoder now accepts known schemas 6–13, requires 18 bytes for v11–v13, and preserves raw payloads for unsupported/truncated schemas. In the upstream packed struct, `em_allocated`, `compiled_with_bwm` and `compiled_with_cep` are adjacent one-bit bool fields sharing payload byte 17: bits 0, 1 and 2 respectively. Schema v12/v13 adds meaning to that packed byte without increasing its byte length. The decoder reads BWM from byte 17 bit 1 and CEP from byte 17 bit 2. A BWM or CEP flag means compiled into that ARM firmware; it does not prove that the module/connector is physically present or that the transport is currently connected.

## CEP and stability fixes

CEP is a PM5↔Flipper Zero path, not a direct PC transport. It uses the PM5 Type-C extended port handshake and then length-prefixed PM3 NG frames over SPI. BWM and CEP are independent capabilities.

Current upstream changelog includes:
- CEP tick-timer fix: `f1cb4952086861f2e27c89fddf0d274269cff9d6`.
- CEP SPI reply corruption fix: `e6d7cd1f9d330b930073f32cda06e308774cd36d`.
- Current changelog also records a PM5 CEP hang fix caused by missing `StartTicks()` before I2C initialisation and the SPI reply corruption fix.

The Control Center only models the source-backed handshake/length prefix. No physical PM5↔Flipper validation is claimed.

## BWM / BLE / Wi-Fi additions

The current upstream command reference and changelog include:
- `hw bwm ble`: radio on/off, pairing/passkey, forgetting bonded devices, TX power and status.
- `hw bwm autooff`: optional idle power-off and unplug policy.
- `hw bwm wifipower`: Wi-Fi modem off and power-save mode.
- `hw bwm powersave`: ESP32 DFS/light-sleep/slow-advertising power-save.
- `hw bwm wifi`: STA + TCP server.
- `hw bwm upgrade`: ESP32 BWM firmware OTA over an existing BWM link.
- BWM flow-control, abort polling and BLE bulk transfer behaviour remain distinct from real-time LF/COTAG streaming.

### Newly found: optional mDNS on BWM

BWM merge commit `b450b1336dfe00fb507efb535ff3d8a1d9d036a9` adds:
- `components/app_wifi_mdns` responder;
- `CONFIG_PM5_MDNS_ENABLE` Kconfig option, default `y`;
- hostname announcement as `<device-identifier>.local`;
- DNS-SD service `_proxmark5._tcp` on the TCP server port.

The Proxmark3 client commit `256f30f0fa7cb2fe84588f3d7ceb5eb3571a3363` adds a connection hint using `tcp:<host>.local:<port>`.

Important discrepancy: the current PM5 BWM usage guide still says there is no mDNS responder. That documentation is stale relative to the newer BWM repo commit and client hint. The Kconfig default does not prove the user's installed binary includes mDNS; treat availability as optional until queried/observed.

Control Center follow-up: add optional DNS-SD discovery of `_proxmark5._tcp`, with fallback to explicit IP/port. Do not assume mDNS is available on all installed BWM firmware or networks.

## Main PM5 firmware flashing over BWM wireless (PR #3650)

PR #3650 is **not** the BWM ESP32-C2 OTA command. It adds a wireless path for flashing the main PM5 firmware through a BWM-aware bootrom. The bootrom runs a polled UART4/app_com bridge, reports `DEVICE_INFO_FLAG_UNDERSTANDS_BWM_STREAM`, and accepts forwarded legacy bootloader commands/data. The host flasher recognises `tcp:`, `udp:` and `bt:` as wireless and refuses to write if the bootrom does not advertise the BWM-stream capability. Upstream's error message says to flash a BWM-capable bootrom over USB once, then retry wireless flashing. Our `WifiTcpTransport` carries PM3-NG commands and is not by itself a compatible bootloader/OLD-frame flasher.

## BWM ESP32-C2 OTA semantics

Upstream `hw bwm upgrade` / `CMD_PM5_BWM_ESP_OTA` is an ESP32-C2 update over the existing BWM app_com link. Its actions include version query, begin with total image size, chunk write, end/finalise/reboot and abort. The current code explicitly checks chunk size against `BWM_OTA_CHUNK_MAX`; image validation checks ESP image magic `0xE9` and ESP32-C2 chip ID `0x000C`.

This is BWM ESP32-C2 OTA, not PM5 ARM/FPGA firmware flashing. Upstream checks image magic `0xE9`, ESP32-C2 chip ID `0x000C`, and app signature `0xABCD5432`; chunks are at most 240 bytes and paced because the AT32↔ESP UART can drop bursts. The host retries the whole OTA up to six times because the BWM OTA path has no resume. A missing END acknowledgement can mean the ESP rebooted; confirm the running version after reconnecting. Do not run on the user's device without exact installed-version identification, a trusted image/checksum, compatibility and recovery assessment, and explicit approval.

## Upstream stability warning

The upstream Proxmark5 README still states that PM5 firmware is actively developed/unstable and says not to install the BWM addon board for now. Preserve the user's already-installed module if their current assembly is working; this is not a general installation recommendation or proof of hardware stability.

## Integration evidence

- Source facts: verified against upstream repository files/commits on 2026-10-02.
- Unit tests: added for schema v12/v13 parsing, truncated/unknown schemas, CEP framing constants and capability gating.
- CI: pending for the new integration PR.
- Physical PM5/BWM/Flipper verification: not performed by this repository.

Research date: 2026-10-02.

## Bootloader Device Info and BWM stream capability

Upstream `include/pm3_cmd.h` defines OLD frames as fixed 544-byte packets: 8-byte command, three 8-byte arguments, and a 512-byte data area. OLD frames have no NG magic/CRC and remain pinned to the 512-byte payload independently of `PM3_CMD_DATA_SIZE`.

The bootloader `CMD_DEVICE_INFO` command is `0x0000`. Its OLD response uses:
- `arg[0]`: device-info flags;
- `arg[1]`: device-info protocol version (the BWM-aware bootrom currently replies with 1);
- `arg[2]`: BWM bridge baud selected by the bootrom, or zero if no ESP answered.

`DEVICE_INFO_FLAG_UNDERSTANDS_BWM_STREAM` is bit 9 (`1 << 9`). This indicates the bootrom can de-frame BWM DATA_FORWARD as a byte stream, including OLD commands spanning BWM frames. A nonzero BWM baud additionally indicates that an ESP answered the bootrom probe. Neither value proves that Wi-Fi/TCP or BLE is currently connected.

The Control Center now has a fixed-size OLD frame codec and a read-only Device Info model. It does not send any flash command, and it does not yet implement the bootloader transport.
