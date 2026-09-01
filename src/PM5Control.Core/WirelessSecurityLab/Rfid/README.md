# RFID Research Engine

Offline tools for generating and analysing card UIDs. **No direct Proxmark communication** — transport clients will be added later.

## Modules

| File | Description |
|------|-------------|
| `FuzzingStrategy.cs` | Generation strategies (BitFlip, Nibble, Sequential, Pattern, Checksum, Prefix) |
| `UidCandidate.cs` | Single UID candidate representation |
| `UidGenerator.cs` | Candidate generator for selected strategies |
| `UidPatternAnalyzer.cs` | Analyses known UIDs (prefix, entropy, sequence, checksums) |
| `PatternReport.cs` | Analysis output model |
| `ChecksumAnalyzer.cs` | Detect / recompute XOR and LRC checksums |
| `ManufacturerPrefixAnalyzer.cs` | Manufacturer detection from first byte |
| `CandidateScorer.cs` | Scores candidates against a pattern report |
| `EvidenceLog.cs` | Audit trail of generation and analysis operations |

## Usage

```csharp
var generator = new UidGenerator();
var baseUid = new byte[] { 0x04, 0xA7, 0x31, 0x12 };
var candidates = generator.GenerateCandidates(
    baseUid,
    FuzzingStrategy.BitFlip | FuzzingStrategy.ChecksumBrute,
    maxCandidates: 500);

var analyzer = new UidPatternAnalyzer();
var report = analyzer.Analyze(knownUids);

var scorer = new CandidateScorer();
var scored = scorer.ScoreCandidates(candidates, report);

var log = new EvidenceLog();
foreach (var c in candidates)
    log.LogGeneration(c, FuzzingStrategy.BitFlip);
```

## Intentionally deferred

- `Pm3SerialTransport` — serial path to PM5
- `Pm5RfidTestClient` / `IRfidTestClient` — hardware emulation probes
- Full ISO14443-A frame parser
- GUI and session management

These modules stay offline-only so they compile without a transport implementation.
