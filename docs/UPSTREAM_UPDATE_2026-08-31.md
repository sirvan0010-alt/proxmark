# Upstream PM5/BWM update — 2026-08-31

## Scope

This update records the current upstream PM5/BWM changes reviewed against:

- `RfidResearchGroup/proxmark3`
- `nieldk/proxmark3`

Our repository is the PM5 Control Center/diagnostic project, not a mirror of the full Proxmark3 firmware tree. Firmware-only upstream changes are therefore tracked here as compatibility/provenance information rather than copied wholesale into the host application.

## Battery charging safety — adopted

Upstream PR #3552 (`ADD charge limit 4100mV`) is merged.

The PM5/BWM firmware adds:

- `CMD_PM5_BWM_SET_VCHG = 0x017D`
- default requested charge-voltage target: **4100 mV**
- AW32001E hardware step: **15 mV**
- effective default after hardware quantisation: **4095 mV / 4.095 V**
- firmware safety ceiling: 4200 mV
- supported firmware range: 3600–4200 mV
- target is re-applied at every boot

The change is intentionally conservative: the charger IC can accept a higher value, but firmware caps the target at 4200 mV and defaults below the previous 4.2 V setting.

### Control Center handling

The host-side command identifier is now recorded as `Pm3CommandCode.Pm5BwmSetChargeVoltage = 0x017D`.

It is **not** included in the read-only probe allow-list because sending it modifies the charger register. The Control Center therefore remains non-destructive during automatic diagnostics.

The BWM hardware catalog now records the configurable charge-voltage capability and the effective 4095 mV default.

## Low-battery protection

The upstream BWM work also includes low-battery warning/shutdown behavior. Current logic uses voltage plus SoC corroboration and debounce to avoid false shutdown from an inaccurate gauge reading. The observed intended behavior is an early warning around 3.5 V and eventual shutdown around 3.3 V after sustained low voltage.

This remains firmware behavior; the Control Center must report it rather than attempt to reproduce or force it.

## Wi-Fi

Upstream now provides a Wi-Fi status operation that reports connection state and IP without reconnecting, plus a stop operation that returns the module to BLE-only operation. This is relevant to our transport diagnostics and should be exposed as status/control only after the device capability is positively identified.

## PM5 HF/LF timeout investigation

Recent upstream development confirms a broader PM5 SSC/USB lockup problem. Reports include repeated `UART:: write time-out` after `hf search` / `lf search`, sometimes requiring a full power cycle.

A PM5-specific upstream PR (#3546) proposed FPGA-independent backstops for stalled SSC waits in FeliCa, ISO14443-B and ISO15693, plus forcing the RF field off after FPGA configuration. That PR is currently closed without being merged, so **do not treat it as stable upstream firmware**.

A separate merged FeliCa change (`cb48c928...`) improves FeliCa demodulation and adds additional timeout/state protections. It is relevant to future firmware compatibility testing, but it is not copied into this host-only repository.

## Other current upstream changes worth monitoring

- Runtime command-tree tab completion, removing drift from the generated vocabulary.
- PM3 frame-size handling changes.
- Restoration of client/firmware capability-version parity.
- `hf xerox view` heap out-of-bounds fix.
- BWM documentation and battery telemetry improvements.

## Safety rule for future synchronization

Do not blindly merge the upstream firmware tree into this repository. For every update:

1. identify the upstream commit/PR;
2. classify it as firmware-only, host-client, protocol, safety, or diagnostic;
3. check whether our Control Center already models the affected command/protocol;
4. port only compatible host-side changes;
5. keep destructive/configuration commands outside the automatic read-only probe;
6. record firmware-only changes as provenance until a firmware-source subtree is intentionally introduced.

## Current decision

**4.1 V charging target is adopted at the Control Center compatibility level, with 4095 mV recorded as the actual hardware-set value.**

The physical PM5 firmware is not flashed by this repository update. No reset, flash, OTA operation, or hardware write is performed by the Control Center as part of this synchronization.
