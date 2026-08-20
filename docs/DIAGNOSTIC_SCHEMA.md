# PM5 Control Center Diagnostic Report Schema

## Purpose

The diagnostic report is the stable boundary between the Inspector and humans, automation and future AI assistants.

It must preserve uncertainty instead of hiding it.

## Implemented runtime schema vs. future target

There are deliberately two layers:

- `docs/DIAGNOSTIC_RUNTIME_SCHEMA.json` describes the **currently implemented** `DiagnosticReport` C# model and is the schema exported by `pm5ctl export-schema`.
- `docs/DIAGNOSTIC_SCHEMA.json` describes the **planned hardware-oriented report shape**. It must not be treated as proof that every listed PM5 field is currently implemented or exposed by hardware.

This separation prevents the documentation from claiming a runtime contract that the current code does not actually emit.

The hardware-oriented schema can be promoted or revised after the first real PM5 session, when we know which fields the device actually exposes.

## Current runtime structure

```text
DiagnosticReport
 ├─ createdAt
 ├─ toolVersion
 ├─ softwareCommit
 ├─ values
 │   └─ DiagnosticValue<T>
 └─ evidence
     └─ DiagnosticEvidence
```

## Diagnostic field

Every important observed value should conceptually contain:

```text
DiagnosticValue<T>
 ├─ value
 ├─ sourceState
 ├─ confidence
 ├─ sourceDescription
 └─ timestamp
```

`sourceState` is one of:

```text
Detected
Reported
Expected
Unknown
```

`confidence` is one of:

```text
High
Medium
Low
Unknown
```

The runtime serializer currently uses the C# enum names. Human-facing documentation may render these as uppercase labels, but the machine schema must match the actual serialized representation.

## Evidence chain

Each evidence record currently preserves:

```text
DiagnosticEvidence
 ├─ timestamp
 ├─ probe
 ├─ transport
 ├─ request
 ├─ response
 ├─ parsedResult
 ├─ latencyMs
 ├─ retries
 ├─ source
 ├─ confidence
 └─ softwareCommit
```

This is intentionally independent from the future hardware-oriented `ProbeResult` model. The evidence chain is the provenance record; the future inspector model may build richer probe summaries around it.

## Important rule

A missing field is not necessarily an error. A PM5 firmware revision may legitimately expose some information while not exposing another item. The report must distinguish:

```text
UNKNOWN      = we could not reliably determine it
UNSUPPORTED  = the available interface explicitly does not support it
ERROR        = the operation should have worked but failed
```

## Baseline identity

The eventual hardware report should record enough information to compare repeated sessions:

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

The runtime schema and the hardware-oriented schema are independent from PM5 firmware versions. Any promoted schema change must be documented so old reports remain interpretable.

## Hardware-first rule

Do not finalize a field as a required PM5 hardware field merely because it appears in the planned schema. After the first real PM5 session, fields will be classified as:

- verified and supported;
- optional;
- unsupported;
- unknown;
- not applicable.
