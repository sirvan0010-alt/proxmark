# PM5 upstream update — 2026-10-06

## Scope

Fresh upstream review focused on Proxmark5 structured diagnostics, BWM/ESP32, CEP, transport reliability and regressions.

## Significant upstream changes

### 1. Structured PM5/BWM battery telemetry

Repository: `RfidResearchGroup/proxmark3`

Commit: `5661f21d6099ac0faf3be52138cea650aa6bd885`

Date: 2026-10-04

Adds `CMD_PM5_BWM_GET_BATTERY` = `0x0184`.

Response is a packed 19-byte `bwm_battery_info_t` containing:

- BWM presence;
- fuel-gauge status;
- SoC;
- battery voltage;
- current;
- remaining capacity;
- full-charge capacity;
- design capacity;
- temperature;
- charger fault;
- charge status;
- health percentage.

The implementation shares the same battery reads with `hw status`, reducing drift between terminal output and machine-readable telemetry.

Evidence: SOURCE / PROTOCOL VERIFIED. Physical PM5 behaviour remains UNKNOWN.

### 2. Structured PM5/CEP status snapshot

Repository: `RfidResearchGroup/proxmark3`

Commit: `ac9c402e13d2cf735fe7348c975d876ebe324010`

Date: 2026-10-04

Adds `CMD_CEP_STATUS` = `0x0185`.

The packed 44-byte response contains:

- CEP active state;
- the 19-byte BWM battery structure;
- a NUL-terminated firmware-version field up to 24 bytes.

The command intentionally returns success even when optional CEP/BWM support is not compiled; absent feature fields are zero. Clients should therefore use `CMD_CAPABILITIES` compiled flags to distinguish "not compiled" from an actual zero value.

Evidence: SOURCE / PROTOCOL VERIFIED. Physical PM5 behaviour remains UNKNOWN.

### 3. Related upstream reliability work

The same current upstream area contains CEP/transport reliability work, including timeout/recovery changes and responsiveness improvements. These must be integrated only after exact diff review and regression tests; source existence is not a hardware claim.

### 4. Current regressions to watch

RRG currently lists PM5-specific open issues including:

- #3680: LF 134.2 kHz TI/HDX read returning nothing and LF commands timing out after several operations;
- #3669: 8-series ID write regression associated with PM5 support;
- #3664: PM5 HF MF autopwn issue;
- #3595: PM5 MIFARE hardnested/Auth1/Auth2 failures;
- #3516: PM5 MifareCIdent hard-hang after RATS.

These are watch items, not proof that the user's device is affected.

## Control Center integration

### Implemented

- Added PM5 command constants `0x0184` and `0x0185`.
- Added both to the explicit read-only probe whitelist.
- Added `Pm5StatusDecoder` for the exact upstream packed layouts.
- Added unit tests for valid battery decoding, CEP decoding, malformed length rejection and read-only command gating.

### Evidence state

The implementation is:

- source-backed;
- protocol-modelled;
- unit-tested after implementation;
- NOT yet CI-verified in this session;
- NOT hardware-verified.

### Next blocker

The next useful step is to connect the structured probes to the existing read-only inspector/diagnostic report path, then add simulator fixtures and transport-level tests. Only after that should the Windows Inspector surface the new fields.

## Important boundary

Do not conflate:

- PM5 ARM/FPGA wireless flashing through a BWM-aware bootrom;
- ESP32-C2 BWM OTA;
- structured read-only PM5/BWM status.

The new commands are diagnostic reads and do not authorize or perform firmware writes.
