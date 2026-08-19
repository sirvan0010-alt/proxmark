# PM5 Control Center Architecture

## Design objective

The application is a device-aware client, not merely a GUI wrapper around a terminal window.

The UI must not contain Proxmark protocol logic. Protocol details belong in adapters/services so that Windows, Ubuntu CLI and future Android clients can share the same core.

The first user experience should be automatic and read-only:

```text
Connect PM5
   ↓
Auto-detect transport
   ↓
Identify device
   ↓
Probe supported read-only information
   ↓
Collect ARM / FPGA / ESP32-BWM / capabilities / power
   ↓
Compare against known compatibility data
   ↓
Generate human + machine readable report
```

A failed or unsupported probe becomes an explicit `UNKNOWN`/`UNSUPPORTED` result; the inspector must continue with independent probes whenever the transport remains usable.

## Why C# / .NET 10

The shared implementation is C#/.NET 10. The repository already contains `PM5Control.Core` targeting `net10.0`. fileciteturn192file0

This choice is deliberate:

- **Windows-first:** the main desktop client can use the mature .NET ecosystem for native desktop integration, asynchronous I/O, networking, serial/USB-adjacent APIs and structured diagnostics.
- **Strong typing:** protocol frames, enums, DTOs, capability models and transport interfaces benefit from compile-time checking. This is particularly important when one incorrect field or CRC calculation can produce a false hardware diagnosis.
- **Shared Core:** protocol, diagnostics and compatibility logic can remain independent of the Windows UI and be reused by the Ubuntu CLI and later clients.
- **Testing:** .NET has a mature unit/integration testing ecosystem and works naturally with the existing GitHub Actions pipeline.
- **Maintainability:** the project is intended to become a long-lived open-source client, not a disposable reverse-engineering script.

Python is still appropriate for optional research scripts and one-off protocol experiments, but it is not the primary implementation. TypeScript/Electron is likewise not the primary architecture because the central engineering problem is hardware/protocol integration rather than a web application. A future web-based UI remains possible if it provides a concrete benefit.

Android is a later milestone. We will not claim complete code sharing until the real PM5 transport and BWM interfaces have been verified.

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

The current Core source tree already separates `Connections`, `Devices`, `Diagnostics` and `Protocols`, providing the intended foundation. fileciteturn191file0

## Hardware model

The project is specifically about a **Proxmark5-class device with a main ARM/FPGA/RF subsystem and an ESP32/BWM subsystem** as described by the current project specification. We must not assume every physical revision exposes every feature.

The following areas are tracked independently:

### Main PM5

- hardware model/revision
- device identity
- USB identity
- ARM firmware
- FPGA image/version
- memory where reliably verifiable
- RF capabilities

### ESP32/BWM

- ESP32/MCU model
- BWM firmware/build
- MAC
- Wi-Fi
- BLE
- TCP/UDP
- MQTT
- OTA capability
- free heap/RAM
- uptime
- NVS/system information
- logs/readiness

### Power

- external power
- battery presence
- voltage
- current/charge state
- charging
- temperature
- percentage only if a reliable gauge exposes it

Unavailable telemetry is `UNKNOWN`, never guessed.

## Device Inspector

The Inspector is the first production component. It must gather information without requiring manual CLI entry.

### Automatic discovery sequence

```text
1. Enumerate candidate transports
2. Identify candidate PM5 device
3. Establish the least invasive supported connection
4. Query identity/version information
5. Discover supported read-only capabilities
6. Query main PM5 information
7. Query BWM/ESP32 information if available
8. Query wireless state/capabilities where supported
9. Query power/battery telemetry where supported
10. Run compatibility checks
11. Generate baseline + diagnostic report
12. Disconnect cleanly
```

The sequence is data-driven rather than hard-coded around one firmware version. Each probe has a timeout, parser, expected evidence and read-only classification.

One failed probe must not cause the inspector to invent a value or unnecessarily abort unrelated diagnostics.

## Compatibility Engine

The Compatibility Engine must answer questions such as:

- Is this firmware appropriate for this hardware revision?
- Does this client know this device revision?
- Is the BWM firmware compatible with the available network functions?
- Which features are verified, unsupported, unknown or experimental?

It must explain why it reached a result and record the source revision/date.

## Protocol adapters

Do not create a single giant Proxmark class.

Use adapters/interfaces so different generations can be isolated. A PM3-compatible mechanism can be reused where appropriate, while PM5-specific behavior remains explicit.

The BWM/ESP32 interface is a separate adapter because its lifecycle, networking and firmware are distinct from the main RFID/ARM subsystem.

## Transport abstraction

The same logical request should not care whether the device is connected by USB, BLE or TCP.

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

The current project documentation describes a binary BWM packet layer with request/response/broadcast concepts and CRC validation. Exact constants and command IDs must remain versioned because PM5/BWM software is evolving.

The communication engine must support both:

```text
request -> correlated response

and

unsolicited broadcast -> event dispatcher
```

These are architectural requirements; exact PM5-specific commands must be verified from source or real hardware before being labelled supported.

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

## Reports

Reports should have both human-readable Markdown and machine-readable JSON output. The report must include client version, timestamp, hardware identity, firmware versions, transport, capabilities, power state where available, compatibility results, warnings and evidence.

## Firmware manager

The Firmware Manager is deliberately separated from diagnostics. Diagnostics can be read-only. Firmware changes are privileged actions with explicit confirmation and a verified backup mechanism.

Never automatically update firmware merely because a newer version exists.

## Network architecture

Future remote operation can use:

```text
Windows / Android
       |
       +---- USB / BLE / TCP ----> BWM
                                      |
                                      | UART/internal link
                                      v
                                    PM5
```

For remote operation over the Internet, prefer a private VPN/overlay network rather than exposing an unverified BWM TCP listener directly to the public Internet.

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
8. golden-frame tests based on verified material
9. real-device integration tests

Every report/test must clearly identify simulation versus physical hardware evidence.

## Future automation

Automation is built above the protocol layer. Workflows express device operations without scattering raw CLI strings throughout the application. This makes workflows easier to maintain when upstream command syntax changes and allows the same logical operations to be surfaced by Windows, Ubuntu and Android clients.

## Design rule

**The UI should be simple because the Core is well structured, not because the Core hides uncertainty.**
