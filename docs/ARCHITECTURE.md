# Architecture

## Design objective

The application is a device-aware client, not merely a GUI wrapper around a terminal window.

The UI must not contain Proxmark protocol logic. Protocol details belong in adapters/services so that Windows and Android can share the same core.

## Layers

```text
+------------------------------------------------------+
| UI                                                   |
| Windows / future Android                             |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Application Layer                                     |
| commands, workflows, ViewModels, user actions       |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Device / Diagnostic Layer                             |
| Inspector | Capability discovery | Compatibility    |
| Report generation | Confidence calculation          |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Protocol Abstraction                                  |
| PM3-compatible | PM5-specific | ESP32/BWM           |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Framing / Event Layer                                 |
| packet codec | request/response | async events         |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Transport                                              |
| USB | serial where applicable | BLE | Wi-Fi/TCP       |
+----------------------------+-------------------------+
                             |
+----------------------------v-------------------------+
| Proxmark5 hardware                                     |
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

## BWM framing and asynchronous events

The current upstream BWM documentation describes a binary packet protocol rather than a text command stream. The protocol has separate request, response and broadcast frame markers and a CRC16-CCITT integrity check.

The exact constants must remain versioned because the PM5/BWM firmware is actively evolving.

The communication engine must support both:

```text
request -> correlated response

and

unsolicited broadcast -> event dispatcher
```

Examples of events include Wi-Fi scan results, forwarded data and system/log messages. Therefore a single `SendAndWaitForReply()` abstraction is not sufficient.

## Suggested conceptual interfaces

```text
IProxmarkTransport
  ConnectAsync()
  DisconnectAsync()
  ReadFramesAsync()
  WriteAsync()
  State

IProxmarkProtocol
  IdentifyAsync()
  GetCapabilitiesAsync()
  GetFirmwareInfoAsync()
  SubscribeEvents()

IBwmProtocol
  GetVersionInfoAsync()
  GetDeviceModelAsync()
  GetSystemInfoAsync()
  GetWifiStatusAsync()
  StartWifiScanAsync()
  SubscribeEvents()
```

These are architectural concepts, not a requirement to freeze exact method signatures before real-device validation.

## Diagnostic result model

Each field should be represented conceptually as:

```text
Value
SourceState: DETECTED | REPORTED | EXPECTED | UNKNOWN
Confidence: HIGH | MEDIUM | LOW | UNKNOWN
SourceDescription
Timestamp
Protocol/Firmware version
Evidence
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

## Network architecture

Future remote operation can use:

```text
Windows / Android
       |
       +---- USB / BLE / TCP ----> BWM
                                      |
                                      | UART
                                      v
                                    PM5
```

For remote operation over the Internet, prefer a private VPN/overlay network rather than exposing the BWM TCP listener directly to the public Internet.

## Test architecture

Protocol code must be testable without hardware.

Minimum test layers:

1. packet codec unit tests
2. CRC tests
3. malformed-frame tests
4. request/response correlation tests
5. asynchronous event tests
6. transport mock tests
7. compatibility-rule tests
8. golden-frame tests based on verified upstream material
9. real-device integration tests

Every report/test should clearly identify whether it was produced by simulation or real hardware.

## Future automation

Automation will be built above the protocol layer. A workflow should be able to express device operations without embedding raw CLI strings throughout the application.

This makes workflows portable between Windows and Android and reduces breakage when upstream command syntax changes.
