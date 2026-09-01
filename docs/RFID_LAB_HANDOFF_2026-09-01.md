# RFID Lab handoff — 2026-09-01

**Audience:** other AIs / developers continuing PM5 Control Center work.  
**Repo:** `sirvan0010-alt/proxmark`

Read this before inventing transport, parsers, or attack wrappers.

---

## What landed today (code on `main`)

### Offline RFID research engine

Path: `src/PM5Control.Core/WirelessSecurityLab/Rfid/`

| File | Role |
|------|------|
| `FuzzingStrategy.cs` | Flag strategies (BitFlip, Nibble, Sequential, Pattern, Checksum, Prefix) |
| `UidCandidate.cs` | Candidate model + equality |
| `UidGenerator.cs` | Offline candidate generation (combined strategies) |
| `UidPatternAnalyzer.cs` | Prefix / entropy / sequential / checksum hints |
| `PatternReport.cs` | Analyzer output |
| `ChecksumAnalyzer.cs` | XOR/LRC detect + recompute |
| `ManufacturerPrefixAnalyzer.cs` | First-byte manufacturer map |
| `CandidateScorer.cs` | Score candidates vs report |
| `EvidenceLog.cs` | Audit trail for offline ops |
| `README.md` | Module overview |

**No hardware I/O.** Compiles without Proxmark connected.

### Protocol stubs

Path: `src/PM5Control.Core/Protocols/Pm3/`

| File | Role |
|------|------|
| `Pm3CommandCodec.cs` | Minimal ASCII encode + rough decode helper |
| `Pm3ResponseCorrelator.cs` | Correlate response; filter debug `0x0100`; uses `IPm3FrameTransport` (not `Connections.IProxmarkTransport`) |

Namespace: `PM5Control.Core.Protocols.Pm3`.

### Docs (this change set)

| File | Role |
|------|------|
| `docs/RFID_LAB_POLICY.md` | Authorized lab + diagnostic truth |
| `docs/RFID_ICEMAN_COMMAND_CATALOG.md` | CLI knowledge backup (PM3/PM5 notes) |
| `docs/RFID_LAB_HANDOFF_2026-09-01.md` | This handoff |

### Cleanup

- Removed accidental `WirelessSecurityLab/proxmark-rfid-engine.zip` from the source tree.

---

## Explicit non-goals (do not implement next)

1. **Mass C# attack clients** (`MifareClassicClient` with nested/hardnested/autopwn/…, `LfClient`, etc.)  
   - Waste without a locked transport.  
   - CLI text ≠ device binary NG API.

2. **Speculative NG command IDs** (e.g. inventing `0x0301` = nested).  
   - Use real headers from the firmware revision in use.

3. **Treating byte-scan (`0x60` anywhere in buffer) as authentication success.**  
   - False positives; forbidden for production conclusions (`RFID_LAB_POLICY.md`).

4. **Mixing NG host-device framing with ISO14443 RF frame parsing.**  
   - Two layers; two parsers later if needed.

5. **Copying entire Iceman `client/src` into this repo.**  
   - License/size/drift. Prefer upstream fork mirror + this catalog doc.

---

## Practical recommendations (current)

1. **Do not** implement full NG/14a parsers or attack clients yet.  
2. **Keep** offline `WirelessSecurityLab/Rfid` as-is.  
3. **Use** the markdown catalog as knowledge backup / handoff.  
4. **Transport:** when ready, extend **one** stack under `Connections` / existing `Pm3*` types — one adapter, no parallel hierarchy.

---

## Architecture constraints (must respect)

- Same **policy / session / evidence** spirit as WirelessLab and BWM work.  
- BWM (ESP8684 Wi-Fi/BT) is **orthogonal** to HF/LF RFID CLI attacks.  
- ARM ↔ BWM bridge may still be incomplete upstream; do not assume wireless RFID control via BWM.  
- Prefer diagnostic honesty over green UI checkmarks.

---

## Existing related code in repo (do not duplicate blindly)

Inspect before writing new I/O:

- `src/PM5Control.Core/Connections/` — `IProxmarkTransport`, serial transports, `Pm3ReadOnlyClient`
- `src/PM5Control.Core/Protocols/Pm3/` — `Pm3NgFrame`, command codes, inspectors
- `src/PM5Control.Core/WirelessLab/` — capability matrices, session validator
- `src/PM5Control.Core/WirelessSecurityLab/` — rogue AP analyzer + new `Rfid/`
- `docs/ARCHITECTURE.md`, `docs/WIRELESS_SECURITY_LAB.md`, BWM docs

---

## When transport is ready — minimal next code

1. Pin firmware + client version in session log.  
2. One `RfidLabSession` (or similar) that can run **identification** commands and store raw evidence.  
3. Optionally 3–5 Classic helpers — not 55.  
4. Structured parse only after comparing live captures to `Pm3NgFrame` / upstream headers.

---

## How another AI should continue

1. Read `RFID_LAB_POLICY.md` + this handoff.  
2. Skim `WirelessSecurityLab/Rfid/README.md`.  
3. Map `Connections` + `Protocols/Pm3` as the real I/O path.  
4. Do **not** re-propose the full attack-wrapper catalog unless the user explicitly requests a **docs-only** extension of `RFID_ICEMAN_COMMAND_CATALOG.md`.  
5. Prefer small, reversible commits aligned with diagnostic truth.
