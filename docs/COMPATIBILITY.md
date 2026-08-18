# Compatibility and Verification

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

## Mismatch policy

If two sources disagree, show both values.

Example:

```text
Memory
  Device reports: 512 KiB
  Hardware profile expects: 1024 KiB
  Direct verification: unavailable

  Result: MISMATCH
  Confidence: MEDIUM

  Possible causes:
    - firmware definition mismatch
    - unsupported hardware revision
    - incomplete PM5 support
    - hardware fault
```

Never silently choose one value.

## Upstream tracking

When documenting compatibility with RfidResearchGroup/proxmark3 or another upstream source, record:

- repository
- branch/tag if relevant
- exact commit when possible
- date checked
- component/path used as evidence

This is required because upstream development is active and structures/API details can change.

## Compatibility database goals

The future database should cover:

- hardware revisions
- firmware versions
- FPGA versions
- ESP32/BWM versions
- transport capabilities
- known memory configurations
- known unsupported features
- known incompatibilities
- verified test results

The database should never turn an unverified assumption into a `DETECTED` result.
