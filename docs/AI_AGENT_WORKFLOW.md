# AI Agent Workflow — Proxmark5

## Mission

Build the Proxmark5 Control Center into a trustworthy, hardware-aware engineering client for a real Proxmark5 device. The system covers diagnostics, device management, protocol support, RFID/NFC capability mapping, automation, usability, security and future feature discovery.

The project must remain evidence-driven even when agents are highly proactive.

## What Proxmark5 means for this project

Proxmark is a general-purpose RFID/NFC/security-research platform. The upstream RfidResearchGroup `proxmark3` codebase is a major reference point, but this project separately models PM5 hardware and its ESP32/BWM subsystem. Therefore every capability must be classified by actual evidence and hardware compatibility.

## Standard agent cycle

For every substantial task:

### A — Execute
Complete the explicitly requested change.

### B — Inspect
Inspect callers, consumers, tests, documentation, compatibility records and affected interfaces.

### C — Improve
Let the specialist agents propose the next non-blocked improvement.

### D — Verify
Run tests/CI and classify what the result actually proves.

### E — Record
Update provenance, assumptions, known limitations and next blocker.

## Feature discovery is continuous

The `FEATURE_ARCHITECT` and `EFFICIENCY` agents should periodically inspect:

- current UI friction;
- repeated manual steps;
- upstream/reference changes;
- missing diagnostics;
- missing compatibility information;
- opportunities for automation;
- test gaps;
- reliability problems;
- security hardening opportunities;
- cross-platform opportunities.

Examples of desirable proposals include:

- automatic device capability reports;
- one-click evidence/export bundles;
- firmware compatibility explanations;
- transport health diagnostics;
- connection recovery;
- protocol trace inspection;
- regression fixture generation;
- capability comparison between firmware versions;
- safe pre-flash readiness checks;
- configuration backup/restore validation;
- anomaly detection in device telemetry;
- searchable command/capability discovery;
- automated documentation drift detection.

These are examples, not claims that the hardware currently supports every function.

## Security-by-design gate

Before implementing a new automated hardware action, the Security Agent must consider:

- Is it read-only or state-changing?
- Can it damage or erase device state?
- Does it affect firmware, calibration, persistent configuration or credentials?
- Is explicit user confirmation required?
- Can it be simulated first?
- Is there a recovery path?
- Can the action be logged and reproduced?
- Does the UI clearly communicate uncertainty and risk?

Read-only diagnostics should be preferred for initial hardware integration.

## Safe automation boundary

Automation may freely improve:

- inspection;
- parsing;
- reporting;
- compatibility checks;
- simulation;
- testing;
- logging;
- evidence management;
- connection handling;
- documentation.

State-changing or security-sensitive hardware operations require stronger evidence, explicit safeguards and appropriate authorization.

## Evidence status

Use these states consistently:

- `DETECTED`
- `REPORTED`
- `EXPECTED`
- `HYPOTHESIS`
- `SIMULATED`
- `UNKNOWN`
- `HARDWARE_VERIFIED`

Never promote a hypothesis merely because it is plausible.

## Conflict resolution

When agents disagree:

1. check direct hardware evidence;
2. check protocol captures;
3. check executable/source evidence;
4. check exact upstream commit/version;
5. check independent documentation;
6. otherwise retain the disagreement as `HYPOTHESIS` or `UNKNOWN`.

Recency alone never resolves a technical conflict.

## Definition of a good agent contribution

A good contribution either:

- fixes a concrete defect;
- removes a verified blocker;
- improves measurable reliability/efficiency;
- increases evidence quality;
- adds a tested capability;
- proposes a useful feature with a concrete implementation path;
- or makes an explicit hardware-verification task easier.

Do not generate feature volume merely to make the repository look active.
