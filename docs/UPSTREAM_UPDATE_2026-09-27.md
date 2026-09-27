# Upstream PM5 transport snapshot — 2026-09-27

## Checked sources

- RfidResearchGroup/proxmark3
- RfidResearchGroup/Proxmark5_BWM_esp32
- RfidResearchGroup/Proxmark5_FlipperZero_FAP

## PM5 capability schema

Current upstream `include/pm3_cmd.h` defines `CAPABILITIES_VERSION 13`.

Append-only fields relevant to this client:

- v9: `max_cmd_data_size`
- v11: `em_size`, `em_allocated`
- v12: `compiled_with_bwm`
- v13: `compiled_with_cep`

The Control Center now decodes through v13. Future versions remain raw/unknown.

Relevant upstream commit:

- `2c8df402f84d4a19e5f5766385fe382268ac9837` — capability version 12.
- `12296f4afbef598771a2412b3e42b6d0b3051504` — current CEP/default-on documentation state.

## CEP / Flipper Zero

Important commits:

- `2b5e3e51ebc09963fcb070f012527e0405befe4f` — PM5 CEP transport for Flipper Zero.
- `16024ed4a183864d7d9e4146e53f47d491aa4cc4` — CEP default-on for PM5, `SKIP_CEP=1` opt-out.
- `12296f4afbef598771a2412b3e42b6d0b3051504` — documentation cleanup/default build guidance.

Source evidence:

- USART1, 2400 8N1, single-wire half-duplex carries STX + `iamf0rupm5`.
- PM5 replies `yes` over the CEP SPI path.
- CC-controller address is `0x47`, status register `0x09`.
- After attach, CEP carries standard PM3 NG frames with a little-endian uint16 length prefix.
- BWM and CEP are independent and may operate concurrently.

Upstream source comments state the handshake/attach path is hardware-verified on a real Flipper Zero, while post-handshake NG-frame transport was not yet hardware-verified in that source baseline. The Control Center therefore models the protocol but does not claim end-to-end hardware verification.

## BWM / wireless

Important upstream PM5 commits in the latest check:

- `39bbf20cce3119bf26d6abaff7453a4222f0d363` — BWM BLE settings/status and pairing controls.
- `465cb47b5a414217f8412ef3348e6c220dd2020d` — byte-window BWM flow control tied to the ESP receive ring.
- `552276c75f032721e37cd0447ea37e014a73186d` — BWM power-save.
- `5e1b04def11dd43422ee81fa9f24771745db6536` — Wi-Fi modem power-save modes.
- `4f4acedbddc0fe0b889754b6b32f523a95420351` — PM5 idle power-save.
- `0cd1fada0e2af09b4adffe214fc664d4251d1ddb` — BWM/PM5 auto-off behaviour.
- `5c86fb47f1619011f51724fbb0b17c6eb1c8066e` / `da3400ff04f9177e7ea806a12afc5b93c45a385e` — wireless abort polling.
- `9038daceadc9c34d954578225105e430ede38014` / `9c12e30b7905ed60e7b5910e65dbcb2e320cc7f7` / `339db44cf1d8d1c4e65f625b077e4a7d67ae66f4` — explicit BWM capability gating.

These are upstream source/commit facts. Only the commits that explicitly report hardware testing are marked as hardware-tested upstream; none are automatically hardware-verified by this project.

## BWM ESP32 repository

Latest inspected commit remains:

`4818511a2b179c61f80f54b5f825428cba51deb8`

The latest BWM repo changes remain the BLE bulk-transfer retry improvement, notification retry changes and UART RX buffer change. No newer merged commit was found in the latest repository history check.

## Integration consequences

1. Capability schema decoding must support v13.
2. BWM and CEP must be independent capability flags.
3. CEP must be represented as PM5↔Flipper transport, not direct PC transport.
4. BWM/BLE flow control, fragmentation, broadcasts and abort remain transport-level concerns.
5. GUI feature visibility must use capabilities, not the PM5 model name alone.
6. Source/CI/hardware evidence must remain separate in reports.

Research date: 2026-09-27.
