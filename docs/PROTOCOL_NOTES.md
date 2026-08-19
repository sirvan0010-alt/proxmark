# PM5 Protocol Notes

**Status:** Observation log / working notes. Not a protocol specification.

## Purpose

This file records behavior observed from a real Proxmark5 or from a clearly identified upstream/reference implementation.

It exists to prevent assumptions from silently becoming protocol facts.

## Evidence rules

Every observation should record:

- date/time;
- PM5 hardware revision, if known;
- ARM firmware version, if known;
- FPGA version, if known;
- ESP32/BWM version, if relevant;
- client commit;
- transport;
- operation/probe identifier;
- request representation;
- response representation;
- parsed interpretation;
- latency;
- retry count;
- confidence;
- source/evidence location.

Sensitive material must be redacted before committing.

## Observation template

```text
Observation ID:
Date/time:
Hardware:
ARM firmware:
FPGA:
ESP32/BWM:
Client commit:
Transport:
Probe:
Request:
Response:
Parsed result:
Latency:
Retries:
Confidence:
Evidence/source:
Notes:
```

## Known-good observations

None yet. The first physical PM5 session will populate this section.

## Unverified hypotheses

Keep hypotheses separate from observations. Do not place guessed packet layouts in the known-good section.

## Protocol evolution

When an observation becomes sufficiently supported:

1. add or update a parser/adapter;
2. add a unit test using the captured evidence where appropriate;
3. update compatibility data;
4. document the capability and evidence level;
5. expose it through CLI/GUI only after the corresponding verification level is clear.
