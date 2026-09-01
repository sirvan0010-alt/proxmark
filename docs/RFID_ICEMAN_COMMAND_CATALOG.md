# RFID / Iceman CLI command catalog (knowledge backup)

**Purpose:** Local knowledge backup and handoff for PM5 Control Center.  
**Not** a reimplementation of Iceman firmware.  
**Not** a mandate to wrap every command in C#.

Upstream references (prefer live upstream over this file if they diverge):

- https://github.com/RfidResearchGroup/proxmark3
- Client sources under `client/src/cmdhf*.c`, `cmdmf*.c`, `cmdlf*.c`, …

## PM3 vs PM5

| Topic | Note |
|-------|------|
| HF/LF CLI names | Largely the same on Iceman-compatible client+firmware |
| Hardware | PM5 form factor, BWM (Wi-Fi/BT) is a **separate** stack — do not mix with `hf mf` |
| Binary NG IDs | Always take from the **firmware revision you flash**, not from guessed constants |
| Stability | BWM / ARM↔BWM bridge may still be incomplete upstream |

Document **CLI strings + intent** here. Lock binary IDs only after a hardware session against a pinned firmware.

## Authorized use only

See `docs/RFID_LAB_POLICY.md`.

---

## Identification (start here)

| Command | Intent |
|---------|--------|
| `hf 14a info` | HF card type, UID, ATQA/SAK |
| `hf 14a read` | Quick read path (not a complete magic detector by itself) |
| `lf search` | Auto-detect common LF types |

---

## MIFARE Classic (Iceman client)

| Command | Intent | Typical duration |
|---------|--------|------------------|
| `hf mf chk` | Try keys / dictionary against sectors | seconds–minutes |
| `hf mf nested …` | Nested key recovery when ≥1 key known | seconds–minutes |
| `hf mf hardnested …` | Hardnested path; can be long | minutes–hours |
| `hf mf darkside` | Darkside-related path where applicable | minutes |
| `hf mf autopwn` | Orchestrated recovery attempt | minutes |
| `hf mf dump` | Dump after keys available | minutes |
| `hf mf restore` | Restore dump to card (lab blanks / authorized) | minutes |
| `hf mf rdbl` / `hf mf wrbl` | Block read/write with key | seconds |
| `hf mf gen3uid` / `gen3blk` / `gen3freeze` | Gen3 / magic helpers | seconds |
| `hf mf ndef*` | NDEF helpers | seconds |

Exact argument syntax changes over time — run `hf mf help` on the client build in use.

---

## MIFARE Plus / DESFire / Ultralight (pointers)

| Family | Example CLI | Notes |
|--------|-------------|-------|
| Plus | `hf mfp …` | Auth, chk, dump, rdbl/wrbl — see `hf mfp help` |
| DESFire | `hf mfdes …` | App/file oriented; not Classic nested |
| Ultralight | `hf mfu …` | Page read/write; different threat model |

---

## ISO14443-A helpers

| Command | Intent |
|---------|--------|
| `hf 14a sim …` | Simulate with given UID/type |
| `hf 14a sniff` | Sniff reader↔card |
| `hf 14a raw …` | Raw frames |
| `hf 14a list` | List captured frames |

---

## LF (examples)

| Command | Intent |
|---------|--------|
| `lf em 410x demod` / `sim` / `brute` | EM410x family |
| `lf hid sim` / `clone` | HID Prox-style |
| `lf awid …` | AWID |
| `lf indala …` | Indala |
| `lf em 4x05 brute` / `lf em 4x50 brute` | EM4x password paths |
| `lf sniff` | LF sniff |

---

## Other HF families (pointers only)

| Family | CLI prefix |
|--------|------------|
| iClass | `hf iclass …` |
| LEGIC | `hf legic …` |
| ISO15693 | `hf 15 …` |
| ISO14443-B | `hf 14b …` |

---

## Suggested lab sequence (when PM5 HF/LF path works)

```text
1) hf 14a info / lf search
2) Branch by technology
3) Classic: chk → nested/hardnested/autopwn only if authorized → dump
4) Save: raw client output + client/firmware version + timestamp
5) Label evidence per RFID_LAB_POLICY.md
```

## C# integration rule (this repo)

- **Do not** mass-generate `MifareClassicClient` with 15–55 methods before transport is stable.
- Prefer one session helper that can send a **pinned** command string **or** a verified NG opcode, log evidence, and surface confidence.
- Offline UID work lives in `src/PM5Control.Core/WirelessSecurityLab/Rfid/` and does not require these CLI commands.
