# PM5 Control Center Compatibility Model

## Principle

A Proxmark5 client must distinguish what is known from what is assumed.

The repository name `proxmark3` is not a compatibility guarantee for Proxmark5.

## Evidence levels

### DETECTED

Directly verified from the connected hardware or a reliable protocol operation.

### REPORTED

Returned by the connected firmware/device. Useful and important, but not necessarily equivalent to direct hardware verification.

### EXPECTED

Derived from a known hardware/firmware compatibility definition.

### UNKNOWN

No reliable determination is currently available.

## Confidence

- HIGH: direct verification or independent sources agree.
- MEDIUM: reliable firmware/device report, but no independent verification.
- LOW: inference from compatibility data.
- UNKNOWN: insufficient evidence.

## Compatibility dimensions

```text
Hardware
 ├─ model/revision
 ├─ board capabilities
 └─ memory/features

Firmware
 ├─ ARM version/build/commit
 ├─ FPGA version/image
 └─ protocol generation

BWM / ESP32
 ├─ firmware/build
 ├─ API/protocol generation
 └─ wireless capabilities

Transport
 ├─ USB
 ├─ BLE
 └─ Wi-Fi/TCP

Client
 └─ PM5 Control Center version
```

## Mismatch policy

If two sources disagree, show both values.

```text
Memory
  Device reports: 512 KiB       REPORTED
  Hardware profile expects: 1024 KiB   EXPECTED
  Direct verification: unavailable

  Result: MISMATCH
  Confidence: MEDIUM
```

Possible causes must remain hypotheses until verified:

- firmware definition mismatch
- unsupported hardware revision
- incomplete PM5 support
- protocol/API difference
- hardware fault

Never silently choose one value.

## Compatibility database

The planned database is:

```text
compatibility/
  hardware.json
  firmware.json
  bwm.json
  protocols.json
  known-issues.json
```

Records should contain, where applicable:

- model/revision
- firmware version/build
- FPGA version
- BWM version
- supported transports
- capabilities
- known memory configuration
- status
- repository/source
- branch/tag
- exact commit when possible
- date checked
- evidence path/URL
- notes

The database should remain mostly empty until evidence is available. We deliberately do not invent PM5 revisions or memory values before the first physical baseline.

## Result categories

```text
VERIFIED
SUPPORTED
PARTIAL
UNKNOWN
MISMATCH
UNSUPPORTED
EXPERIMENTAL
```

Every non-obvious result should explain why it received its category.

## Upstream tracking

When documenting compatibility with RfidResearchGroup/proxmark3 or another upstream source, record repository, branch/tag if relevant, exact commit when possible, date checked and component/path used as evidence. This is required because upstream development is active and structures/API details can change.

## First hardware rule

The first physical PM5 session establishes evidence. Compatibility definitions are updated from observed data and verified source material, not from assumptions.
