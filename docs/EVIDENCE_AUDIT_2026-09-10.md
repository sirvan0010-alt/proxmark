# PM5 Control Center — Implementation Evidence Audit

Date: 2026-09-10  
Repository: `sirvan0010-alt/proxmark`  
Target branch: `main`

## Purpose

This audit marks the existing implementation areas as either **DOLOŽENO** or **NEDOLOŽENO** against:

1. the current PM5 upstream documentation from `RfidResearchGroup/proxmark3`;
2. the upstream PM5/BWM firmware sources where explicitly referenced;
3. the repository's own evidence rules.

**Important:** DOLOŽENO here means that the implementation has a concrete source/protocol basis. It does **not** mean that the feature has been verified on the user's physical Proxmark5. Physical verification remains a separate state.

The repository itself explicitly requires the distinction between protocol/source evidence and hardware evidence. `docs/COMPATIBILITY.md` defines `DETECTED`, `REPORTED`, `EXPECTED`, and `UNKNOWN`, and requires provenance for compatibility claims. `docs/DEEP_AUDIT_2026-08-21.md` also states that actual PM5 hardware revision, firmware, FPGA, BWM payloads and observed transport behaviour remain deferred until the first physical session.

## Status key

- **DOLOŽENO** — backed by an identifiable upstream source, protocol specification, or deterministic repository-level implementation/test evidence. No claim of physical PM5 verification is implied.
- **NEDOLOŽENO** — implementation exists, but the repository currently lacks sufficient PM5-specific evidence to claim that the implementation represents working PM5 hardware behaviour.
- **SIMULACE** — deliberately offline/test behaviour; not evidence about real hardware.

## Audit matrix

| Area / implementation | Status | Evidence | What is still missing |
|---|---|---|---|
| PM3/PM5 NG frame structure and parser | **DOLOŽENO** | Upstream `new_frame_format.md` defines NG command/response fields, magic, length, command, status/reason, payload and CRC behaviour. Repository implements `Pm3NgFrame` and tests. | Physical PM5 capture confirming the exact transport path used by this application. |
| PM3 command identifiers used by the read-only probe | **DOLOŽENO** | `Pm3CommandCode.cs` explicitly defines `CMD_VERSION`, `CMD_STATUS`, `CMD_PING`, `CMD_CAPABILITIES` and other read-only queries. | Physical PM5 response capture for every command. |
| Read-only command allow-list | **DOLOŽENO** | `Pm3CommandCode.IsSafeReadOnlyProbe()` explicitly restricts commands; `Pm3ReadOnlyClient` rejects commands outside the list. | Physical-device verification that the selected commands are accepted by the target PM5 firmware. |
| Read-only transaction serialization / timeout handling | **DOLOŽENO** | `Pm3ReadOnlyClient` serializes transactions and has explicit timeout/cancellation handling. This is application logic, not a hardware claim. | Runtime test against physical PM5. |
| `CMD_VERSION` response decoding | **DOLOŽENO** | `Pm3ReadOnlyInspector.DecodeVersion()` implements the documented response structure and preserves UNKNOWN when fields cannot be decoded. | Physical PM5 capture proving the returned payload layout on the target firmware. |
| `CMD_CAPABILITIES` v6 decoding | **DOLOŽENO** | `Pm3ReadOnlyInspector` documents the v6 layout and decodes the 13-byte structure and feature bits. | Physical PM5 capture and confirmation against the exact firmware build. |
| PM5 hardware identity detection | **NEDOLOŽENO** | The inspector deliberately reports `PM3-family ARM endpoint verified; hardware family not yet confirmed`. Repository audit says actual PM5 revision is deferred until hardware. | First physical PM5 session with hardware/revision evidence. |
| FPGA identity verification | **NEDOLOŽENO** | `CMD_VERSION` has a decoder path, but repository has no physical PM5 evidence establishing the target's actual FPGA value. | Real PM5 capture and independent cross-check. |
| Hardware memory/capability truth | **NEDOLOŽENO** | Compatibility model explicitly forbids inventing PM5 memory values; reported capability data is not equivalent to direct hardware verification. | Physical PM5 baseline and reconciliation of reported/expected values. |
| PM5 firmware compatibility registry | **DOLOŽENO as a model; not hardware verified** | `docs/COMPATIBILITY.md` defines the evidence model and conservative registry rules. | Real hardware observations and exact firmware/commit mapping. |
| BWM frame format | **DOLOŽENO** | `docs/BWM_PROTOCOL.md` ties the implementation to `RfidResearchGroup/Proxmark5_BWM_esp32` commit `b918166128e05455c2dcb4e232216d453bbf29ee`; magic, CRC, header and command provenance are explicitly recorded. | Physical PM5+BWM wire capture. |
| BWM CRC16 implementation | **DOLOŽENO at protocol/source level** | Repository records the exact upstream CRC polynomial, initial value and scope and states it was checked against both TX and RX firmware paths. | Hardware/on-wire reproduction on the actual PM5/BWM. |
| BWM command code catalogue | **DOLOŽENO at source level** | Repository records 1:1 provenance from upstream `app_com_defs.h`. | Physical command/response validation on the actual BWM firmware version. |
| BWM streaming frame parser | **DOLOŽENO at protocol level** | `BwmStreamParser` exists and has dedicated parser tests; repository protocol document requires fragmented/multiple/invalid-frame handling. | Physical BWM traffic capture. |
| BWM read-only adapter | **DOLOŽENO at protocol/application level** | Explicit read-only allow-list, command provenance and tests exist. The repository audit explicitly requires mutating commands to be rejected before transport. | Real PM5/BWM session proving the commands and payload layouts. |
| BWM response correlation / event dispatch | **DOLOŽENO at protocol/application level** | `BwmReadOnlyAdapter`, `BwmEventDispatcher`, frame codec and tests cover correlation/events. | Physical asynchronous event stream validation. |
| BWM battery/charger telemetry | **NEDOLOŽENO** | Repository correctly states that component presence does not prove telemetry exposure. PM5 upstream documentation currently warns that BWM is not fully supported. | Physical device and exact firmware payload evidence. |
| BWM charger-voltage mutation | **NEDOLOŽENO / intentionally blocked** | `Pm5BwmSetChargeVoltage` is explicitly excluded from the safe read-only allow-list because it changes a charger register. | A separate, deliberately controlled write-path review; never treat presence of the constant as proof of safe operation. |
| BWM Wi-Fi status/configuration | **NEDOLOŽENO for physical PM5** | Code/catalogue and protocol documentation describe read-only possibilities, but there is no physical PM5 evidence in this repository. | Physical BWM firmware response capture. |
| BWM BLE status/control | **NEDOLOŽENO for physical PM5** | Desktop BLE UI/transport implementation exists, but repository evidence does not establish that it is a working PM5/BWM path on the target hardware. | Physical BWM/BLE test and firmware-specific evidence. |
| BWM network forwarding | **NEDOLOŽENO** | Documentation distinguishes local UART, BWM forwarding and remote client, but the repository has no physical proof of the complete PM5 path. | End-to-end physical test. |
| Windows BLE transport | **NEDOLOŽENO as PM5 hardware capability** | `WindowsBleProxmarkTransport.cs` is an application implementation. Existence of transport code is not proof that the connected PM5 exposes the expected BLE interface. | Actual PM5/BWM BLE session. |
| Desktop diagnostic UI | **DOLOŽENO as application code** | `Pm5ReadOnlyConsoleForm` and related forms exist and are connected to the application architecture. | Physical end-to-end acceptance test. |
| CLI diagnostic entry point | **DOLOŽENO as application code** | `PM5Control.Cli/Program.cs` exists and builds against the core. | Physical PM5 acceptance test. |
| Simulator | **SIMULACE** | `PM5Control.Simulator` and fault-injection tests explicitly model offline behaviour. Repository audit states simulator output must never become hardware evidence. | Nothing required for simulator status; hardware verification must be performed separately. |
| Simulator BWM transport | **SIMULACE** | `BwmSimulatedTransport`, `PM5SimulatedDevice`, `DeviceState` and `SimulationFault` provide deterministic test behaviour. | Not applicable; must remain marked simulated. |
| Diagnostic report model/export | **DOLOŽENO as data model** | `DiagnosticReport`, `DiagnosticValue` and exporter exist; repository truth model requires provenance. | Physical evidence to populate real hardware fields. |
| Firmware selection/routing | **NEDOLOŽENO for automatic PM5 use** | Repository documentation deliberately keeps firmware selection disabled until hardware identity is established. Upstream PM5 documentation confirms PM5-specific firmware/`PLATFORM=PM5` requirements. | Physical identity + exact firmware compatibility evidence. |
| Firmware flashing | **NEDOLOŽENO as an implemented safe operation** | Upstream documentation describes PM5 flashing/recovery, but this repository does not yet have evidence sufficient to claim that its own UI/logic safely performs the complete operation. | Dedicated implementation, dry-run validation and later controlled physical test. |
| DFU recovery | **NEDOLOŽENO as an application feature** | PM5 upstream documentation provides the DFU recovery procedure, but repository evidence does not establish an implemented and tested DFU workflow in Control Center. | Explicit DFU module + physical recovery test. |
| FPGA update/configuration | **NEDOLOŽENO as an application feature** | Upstream PM5 documentation describes `hw fpga config`; repository does not currently establish a tested Control Center implementation of the complete operation. | Implement only after hardware identity/firmware routing is proven. |
| PM5 factory-data read/write | **NEDOLOŽENO** | Upstream PM5 documentation documents `hw factorydata`; repository evidence does not establish a corresponding safe Control Center implementation. | Dedicated read-only first; write path separately gated. |
| PM5 antenna/Q configuration | **NEDOLOŽENO** | Upstream PM5 documentation documents PM5-specific hardware commands, but this repository has no sufficient evidence for a safe implemented operation. | Hardware-specific implementation and guarded physical test. |
| Standalone PM5 modes | **NEDOLOŽENO / should remain disabled** | Current upstream PM5 documentation says standalone modes are disabled for debugging. | Do not expose as supported until upstream status changes and physical verification exists. |
| General RFID tag commands | **NEDOLOŽENO in this Control Center** | The repository is a control/diagnostic layer; no complete RFID command implementation is evidenced in the current source tree. Upstream PM3 capabilities cannot automatically be promoted to PM5 claims. | Separate protocol/feature audit before adding any RFID operation. |

## Result

### Clearly supported by source/protocol evidence

The following are the strongest current implementation foundations:

1. PM3/PM5 NG frame structure and response correlation.
2. Read-only command allow-list and transaction safety.
3. `CMD_VERSION` and `CMD_CAPABILITIES` decoding at the protocol level.
4. BWM frame format and CRC at source/protocol level.
5. BWM command provenance.
6. BWM parser/correlation/event machinery at application/protocol level.
7. Simulator and deterministic fault testing, explicitly marked as simulation.
8. Evidence/compatibility/diagnostic data model.

### Not yet proven on real PM5 hardware

The following must **not** be presented to the user as working PM5 hardware functionality yet:

- actual PM5 hardware revision detection;
- exact PM5 firmware/FPGA identity on a physical unit;
- BWM physical communication;
- BWM battery telemetry;
- BWM Wi-Fi/BLE/network operation;
- automatic firmware selection/flashing;
- DFU recovery through Control Center;
- FPGA update through Control Center;
- factory-data write;
- PM5 antenna/Q configuration;
- standalone modes;
- general RFID operations implemented merely because they exist upstream.

## Upstream PM5 boundary

The current upstream PM5 documentation explicitly requires PM5-specific firmware compilation (`PLATFORM=PM5`), warns against conflicting RDV4 `PLATFORM_EXTRAS`, describes PM5 DFU recovery and flashing, and currently warns that BWM is not fully supported. It also states that standalone modes are disabled for debugging. Therefore those upstream facts are valid evidence for what the firmware project documents, but they are **not** evidence that our Control Center already implements those operations.

## Decision

**Do not remove or rewrite the existing protocol implementations merely because they are not hardware verified.** Their provenance is useful. Instead, the application must expose their evidence state honestly:

`PROTOCOL VERIFIED` != `HARDWARE VERIFIED`

The next physical PM5 session should be treated as a controlled evidence-collection session. Its output should upgrade individual rows from `NEDOLOŽENO` to `HARDWARE VERIFIED` only when the observation is actually captured and reproducible.
