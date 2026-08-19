# PM5 Control Center Diagnostic Report Schema

## Purpose

The diagnostic report is the stable boundary between the Inspector and humans, automation and future AI assistants.

It must preserve uncertainty instead of hiding it.

## Output formats

The Inspector will eventually export:

- JSON for machine processing;
- Markdown for humans;
- raw evidence/log files where appropriate.

The exact JSON Schema should be generated after the first real PM5 session, when we know which fields the hardware actually exposes. This document defines the semantic contract without inventing hardware fields.

## Top-level structure

```text
DiagnosticReport
 ├─ schemaVersion
 ├─ reportId
 ├─ timestamp
 ├─ client
 ├─ host
 ├─ device
 ├─ transport
 ├─ firmware
 ├─ bwm
 ├─ capabilities
 ├─ power
 ├─ compatibility
 ├─ probes
 ├─ warnings
 └─ evidence
```

## Diagnostic field

Every important observed value should conceptually contain:

```text
DiagnosticValue<T>
 ├─ value
 ├─ sourceState
 ├─ confidence
 ├─ sourceDescription
 ├─ timestamp
 ├─ protocolVersion
 ├─ firmwareVersion
 └─ evidence
```

`sourceState` is one of:

```text
DETECTED
REPORTED
EXPECTED
UNKNOWN
```

`confidence` is one of:

```text
HIGH
MEDIUM
LOW
UNKNOWN
```

## Probe result

Each automatic probe should record:

```text
ProbeResult
 ├─ probeId
 ├─ category
 ├─ status
 ├─ durationMs
 ├─ sourceState
 ├─ confidence
 ├─ values
 ├─ error
 └─ evidence
```

Possible status values:

```text
SUCCESS
UNSUPPORTED
TIMEOUT
TRANSPORT_ERROR
MALFORMED_RESPONSE
PERMISSION_ERROR
UNKNOWN
```

## Important rule

A missing field is not necessarily an error. A PM5 firmware revision may legitimately expose some information while not exposing another item. The report must distinguish:

```text
UNKNOWN      = we could not reliably determine it
UNSUPPORTED  = the available interface explicitly does not support it
ERROR        = the operation should have worked but failed
```

## Baseline identity

The report should record enough information to compare repeated sessions:

- repository/client version;
- report schema version;
- timestamp;
- host OS;
- USB identity;
- device identity;
- firmware versions;
- BWM version;
- transport;
- probe results.

## Security/privacy

Reports must not automatically include secrets such as Wi-Fi passwords, private keys or authentication tokens. Sensitive network identifiers should be redacted or explicitly classified before sharing publicly.

## Versioning

`schemaVersion` is independent from PM5 firmware versions. A report schema change must be documented so old reports remain interpretable.

## Hardware-first rule

Do not finalize a field as a required PM5 hardware field merely because it appears in an early design document. After the first real PM5 session, fields will be classified as:

- verified and supported;
- optional;
- unsupported;
- unknown;
- not applicable.
