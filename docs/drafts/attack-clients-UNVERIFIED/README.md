# NEKOMPILOVAT / NEOVĚŘENO

Soubory v této složce **nejsou** součástí build path (`src/PM5Control.Core/...`).
Jsou to **návrhy k budoucímu auditu**, uložené kvůli referenci — **nejsou připravené k použití**.

## Proč jsou tady, ne v `Connections/`

* Syntaxe většiny příkazů (`lf hid`, `lf awid`, `hf iclass rdbl/wrbl`, `hf mf *`, `hf mfp *`, `hf legic *`) je **neověřená** proti aktuální verzi firmwaru/klienta — viz `docs/RFID_ICEMAN_COMMAND_CATALOG.md` pro knowledge backup.
* ASCII `"hf mf ...\n"` psané přímo na `IProxmarkTransport` **není automaticky** totéž co binární rámec PM3-NG (`Pm3NgFrame`, `Pm3ReadOnlyClient`). Textový režim vs NG framing je potřeba ujasnit před napojením na transport.
* Chybí allow-list ve stylu `Pm3CommandCode.IsSafeReadOnlyProbe`.
* Odpovídá `docs/RFID_LAB_POLICY.md` a `docs/RFID_LAB_HANDOFF_2026-09-01.md`: neimplementovat mass-attack klienty ani spekulativní parsery, dokud není hotový jeden I/O stack.

## Co už je v těchto souborech opravené

* Chybný namespace `PM5Control.Core.Protocol` / mrtvý `_parser` → odstraněn.
* `UnbrickMagicCardAsync` (`Hf14aClient`) → `NotSupportedException` (původní sekvence byla nesprávná).
* `lf hid sim` → `-r`; `hf iclass dump` → `-f` (ověřit znovu na vašem klientovi).
* `FreezeGen3Async`, `SetGen3UidAsync`, `RestoreCardAsync` vyžadují explicitní `confirmed...: true`.

## Co zůstává NEOVĚŘENO

* Zbytek `LfClient` (brute/clone a další).
* `hf iclass rdbl` / `wrbl`.
* Většina `hf mf` / `hf mfp` / `hf legic` cest.

Ověření: `<command> -h` na **vašem** klientovi.

## Než se cokoliv přesune do `Connections/`

1. Rozhodnutí pro **live větev** (`EmulationProbeEngine` + transport). Offline `WirelessSecurityLab/Rfid` zůstává.
2. Ověřit syntaxi přes `-h`.
3. Ujasnit ASCII vs PM3-NG.
4. Allow-list + **UI-level** potvrzení pro nevratné operace.
5. Session / evidence dle `RFID_LAB_POLICY.md`.

## Soubory

| Soubor | Poznámka |
|--------|----------|
| `MifareClassicClient.cs` | Classic + gen3 gates; většina syntaxe NEOVĚŘENO |
| `Hf14aClient.cs` | Unbrick = NotSupportedException |
| `LfClient.cs` | Částečné opravy; brute/clone NEOVĚŘENO |
| `HfIclassClient.cs` | dump -f; rdbl/wrbl NEOVĚŘENO |

**Build:** tyto soubory nepatří do `.csproj` / `src/`.
