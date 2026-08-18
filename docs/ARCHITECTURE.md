# Architecture

## Design objective

The application is a device-aware client, not merely a GUI wrapper around a terminal window.

The UI must not contain Proxmark protocol logic. Protocol details belong in adapters/services so that Windows and Android can share the same core.

## Layers

```text
+------------------------------------------------------+
| UI                                                 |
| Windows / future Android                           |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Application Layer                                  |
| commands, workflows, ViewModels, user actions      |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Device / Diagnostic Layer                          |
| Inspector | Capability discovery | Compatibility  |
| Report generation | Confidence calculation         |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Protocol Abstraction                                |
| PM3-compatible | PM5-specific | ESP32/BWM          |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Transport                                           |
| USB | serial where applicable | BLE | Wi-Fi/TCP     |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Proxmark5 hardware                                  |
+------------------------------------------------------+
```

## Device Inspector

The Inspector is the first production component. It must gather information without requiring manual CLI entry.

Responsibilities:

- discover transports
- identify device
- query firmware versions
- query hardware capabilities
- collect memory information
- inspect BWM/ESP32 state
- inspect power/battery telemetry
- compare results against compatibility data
- assign source states and confidence
- create reports

The Inspector should be able to run in a read-only mode.

## Compatibility Engine

The Compatibility Engine must answer questions such as:

- Is this firmware appropriate for this hardware revision?
- Does this client know this device revision?
- Is the BWM firmware compatible with the available network functions?
- Which features are verified, unsupported, unknown or experimental?

It must explain why it reached a result.

## Protocol adapters

Do not create a single giant Proxmark class.

Use adapters/interfaces so different generations can be isolated. A PM3-compatible mechanism can be reused where appropriate, while PM5-specific behavior remains explicit.

The BWM/ESP32 interface is a separate adapter because its lifecycle, networking and firmware are distinct from the main RFID/ARM subsystem.

## Transport abstraction

The same logical request should not care whether the device is connected by USB, BLE or TCP.

Example conceptual flow:

```text
GetDeviceInfo()
      |
Protocol Adapter
      |
Transport
      |
Proxmark5
```

The transport layer returns structured data/errors rather than terminal text where possible.

## Diagnostic result model

Each field should be represented conceptually as:

```text
Value
SourceState: DETECTED | REPORTED | EXPECTED | UNKNOWN
Confidence: HIGH | MEDIUM | LOW | UNKNOWN
SourceDescription
Timestamp
```

This is important for resolving cases such as firmware reporting 512 KiB while a known hardware revision expects another value.

## Reports

Reports should have both:

1. human-readable Markdown/text output;
2. machine-readable JSON output.

The report must include client version, timestamp, hardware identity, firmware versions, transport, capabilities, power state where available, compatibility results and warnings.

## Firmware manager

The Firmware Manager is deliberately separated from diagnostics.

Diagnostics can be read-only. Firmware changes are privileged actions with explicit confirmation and a backup check.

Never automatically update firmware simply because a newer version exists.

## Future automation

Automation will be built above the protocol layer. A workflow should be able to express device operations without embedding raw CLI strings throughout the application.

This makes workflows portable between Windows and Android and reduces breakage when upstream command syntax changes.
