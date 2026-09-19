# Upstream PM5 transport snapshot — 2026-09-19

Checked against:

- `RfidResearchGroup/proxmark3`
- `RfidResearchGroup/Proxmark5_BWM_esp32`

## PM5 / PM3 NG payload size

Upstream `include/pm3_cmd.h` currently defines:

- `PM3_CMD_DATA_SIZE = 4064` for PM5 firmware and the client.
- legacy/OLD frames remain pinned at 512 bytes.
- `PM3_FPC_MAX_DATA = 2048` for forwarding through the BWM/FPC path.

Relevant upstream commit:

- `2c955d7deb8a03d0d66b382f9dcf2834fd42b36a` — fixes a PM5 compatibility regression after increasing `PM3_CMD_DATA_SIZE`; the transmit buffer must accommodate the larger PM5 command data size and double buffering.

PM5 Control Center previously capped PM3 NG payloads at 512 bytes. That was too restrictive for the current PM5 definition. The Core framing limit is now 4064 bytes, with the legacy 512-byte and BWM/FPC 2048-byte limits kept explicit for future transport-specific enforcement.

## PM5 SPI/FPGA stability

Upstream:

- `38e06eec5ad553547a8f888ec6248af37767ef1f` — fixes an AT32 CSPAS error path that could leave SPI/SSC hung and cause random PM5 freezes.
- `d5ef82658740a6cd4843cfb27fc8a28166e5a717` — checks SSC transmitter readiness before sending.

The Control Center must not classify every missing response as a simple unplug/disconnect. A future hardware diagnostic layer should distinguish transport timeout, device disconnect, and PM5-side SPI/SSC stall when hardware evidence is available.

## BWM / ESP32

Current latest commit in `RfidResearchGroup/Proxmark5_BWM_esp32` remains:

- `4818511a2b179c61f80f54b5f825428cba51deb8` — `Fix BLE bulk-transfer drops`.

The preceding changes reduced UART RX buffering to 4096 bytes and changed notification retry behaviour. No newer BWM commit was found in this check.

This means the Control Center should continue to treat BWM/BLE as a potentially fragmented and lossy transport and keep BWM framing, PM3 framing, and response correlation as separate layers.

## Release note

The upstream `v4.23346` release was created and then reverted at the commit level. Do not treat the reverted release commit itself as the current PM5 baseline. The individual fixes listed above are the relevant evidence.

## PM5 Control Center impact

1. PM3 NG framing now accepts the full upstream PM5 4064-byte payload.
2. Regression tests cover exactly 4064 bytes and reject 4065 bytes.
3. Legacy 512-byte and BWM/FPC 2048-byte limits are explicit constants rather than hidden assumptions.
4. BWM forwarding remains separated from PM3 NG parsing.
5. SPI/SSC freeze handling remains a diagnostic/evidence item; it is not falsely reported as verified without hardware evidence.

Source commits were checked on 2026-09-19. This document records upstream evidence, not physical-hardware verification.
