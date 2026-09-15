# PM5 BWM Abort Evidence — 2026-09-15

## Upstream evidence

Repository: `RfidResearchGroup/Proxmark3`
PR: #3629
Merge commit: `da3400ff04f9177e7ea806a12afc5b93c45a385e`
Implementation commit: `5c86fb47f1619011f51724fbb0b17c6eb1c8066e`

Title: `Fix: poll the BWM link in data_available(), so a Proxmark5 on BLE/WiFi can be aborted`

The upstream firmware previously polled USB, and on some builds the FPC UART, when deciding whether a running device-side loop should stop. A PM5 build with `PLATFORM_EXTRAS=BWM` defines `WITH_BWM_FORWARD`, but the abort path did not poll the BWM link. Consequently `CMD_BREAK_LOOP` sent over BLE/Wi-Fi was not observed by the running loop.

The merged fix adds the BWM branch to both `data_available()` and `data_available_fast()` using `bwm_fwd_rxdata_available()`, while preserving USB-first ordering. Upstream reports that non-BWM object code remains byte-identical and that the fix was exercised on physical PM5+BWM hardware over BLE.

## Control Center adoption

The Windows Control Center must mirror the **behavioural contract**, not copy firmware code:

```text
running operation
      ↓
user cancellation
      ↓
transport-level abort path
      ↓
CMD_BREAK_LOOP
      ↓
PM5 exits device-side loop
      ↓
wireless/serial session remains usable
```

`CMD_BREAK_LOOP` is recorded in `Pm3CommandCode.BreakLoop = 0x0118`, based on upstream `include/pm3_cmd.h`. It is deliberately excluded from the read-only probe allow-list because it is a control operation, not a diagnostic query.

The shared core now exposes an optional `IProxmarkAbortTransport` capability and `Pm3ReadOnlyClient` attempts the abort when caller cancellation occurs. The existing PM3 serial transport implements this capability.

## Important boundary

The current `WindowsBleProxmarkTransport` is a BWM GATT transport for the BWM command framing. It must **not** be changed to inject a raw PM3 NG frame until the exact transparent-forwarding wire path is verified. Upstream PR #3629 proves that the PM5 firmware must observe `CMD_BREAK_LOOP` on the BWM forwarding path; it does not prove that a BWM control frame carrying a PM3 command is the correct Windows BLE wire operation.

Therefore:

- PM3 serial abort: implemented at protocol/transport level.
- PM5+BWM BLE abort contract: documented and architecturally prepared.
- Windows BLE end-to-end abort: NOT YET HARDWARE VERIFIED.
- No guessed BWM command ID is introduced.

## Related upstream defect intentionally tracked separately

PR #3629 explicitly notes a separate issue: `ReadLF_realtime()` writes samples directly to USB bulk-IN without a BWM/FPC branch. Large `lf read` / `lf sniff` / `lf cotag read` operations over BLE/Wi-Fi therefore remain a separate transport/data-path problem. Do not mark that path fixed merely because `CMD_BREAK_LOOP` is now handled upstream.
