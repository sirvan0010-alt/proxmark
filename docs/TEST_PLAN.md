# PM5 Control Center Test Plan

## Principle

Testing must progress from deterministic offline tests to read-only real-device tests and only much later to mutating operations.

## Phase T0 — Offline protocol tests

No hardware required.

- CRC16-CCITT known vectors
- valid request frame encoding
- valid response frame decoding
- broadcast frame decoding
- truncated frame rejection
- invalid magic rejection
- invalid length rejection
- invalid CRC rejection
- oversized payload rejection
- fragmented stream reassembly
- multiple frames in one read
- request/response correlation
- unsolicited event dispatch
- timeout handling
- duplicate/late response handling

## Phase T1 — Compatibility tests

No hardware required.

- known hardware definition loads correctly
- known firmware definition loads correctly
- unknown hardware remains UNKNOWN
- EXPECTED values are never reported as DETECTED
- mismatched memory is reported as mismatch
- unsupported capability is not shown as available
- stale compatibility entries can be detected by source revision/date

## Phase T2 — Transport mocks

No hardware required.

Simulate:

- USB byte stream
- TCP stream
- BLE packet stream where framing permits
- disconnect/reconnect
- partial reads
- delayed packets
- connection refusal
- malformed frames

The protocol layer must behave identically regardless of mock transport.

## Phase T3 — First real PM5 session

**Read-only only. No firmware update.**

Before connecting:

1. record project/client version;
2. record the upstream research snapshot;
3. prepare a clean diagnostic output directory;
4. verify that no update action is automatically triggered.

Then:

1. connect USB;
2. identify VID/PID;
3. identify PM5 where possible;
4. read existing firmware/version information;
5. inspect FPGA information where supported;
6. detect BWM/ESP32;
7. read BWM version/model/system information;
8. query only documented read-only capabilities;
9. inspect Wi-Fi/BLE state without changing configuration;
10. inspect power/battery telemetry where exposed;
11. export a complete report;
12. disconnect cleanly.

## T4 — Repeatability

Run the same diagnostic several times and compare reports.

Expected:

- stable hardware identity;
- stable firmware versions;
- stable capability results;
- predictable dynamic fields (uptime, heap, battery level) clearly marked as dynamic.

## T5 — Network transport

Only after USB read-only operation is verified.

- verify BWM TCP server behavior on a private LAN;
- verify connection/disconnection;
- run read-only diagnostics over TCP;
- compare results with USB;
- verify that request/response and asynchronous events remain correct.

Do not expose a raw BWM listener to the public Internet.

## T6 — BLE transport

Only after USB and TCP foundations are stable.

- discover device;
- establish supported BLE transport;
- run read-only identification;
- compare results against USB;
- test disconnect/reconnect.

## T7 — Mutating operations

Only after backups, compatibility rules and explicit confirmation UX exist.

Examples:

- Wi-Fi configuration
- BWM configuration
- profiles
- firmware update

Every mutating test must have a rollback/recovery plan.

## Test result labels

Use:

- `PASS`
- `FAIL`
- `NOT TESTED`
- `NOT SUPPORTED`
- `UNKNOWN`
- `REQUIRES REAL HARDWARE`

Never convert `NOT TESTED` into `PASS` merely because the implementation compiles.
