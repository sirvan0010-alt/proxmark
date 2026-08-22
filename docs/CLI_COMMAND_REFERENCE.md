# Proxmark CLI Command Reference — PM3 vs PM5

> **Evidence level:** PM3 commands below are documented upstream/reference commands. They are **not** evidence that the same command exists or is safe on a physical PM5. PM5-specific command support must be detected/verified on the actual device.

## Why this file exists

The Control Center is intended to remove the need for the user to type CLI commands. The commands are nevertheless useful as a reference when validating the Inspector and when comparing the PM3 software ecosystem with PM5.

The key distinction is between:

1. **CLI commands** — commands entered into a Proxmark client;
2. **transport/protocol commands** — packets exchanged between client and firmware;
3. **BWM command IDs** — commands used by the ESP32/BWM firmware API.

These are not interchangeable.

## PM3/reference commands

The established PM3 client exposes a top-level `help` command and command families such as `hw`, `lf`, `hf` and `data`. The upstream command documentation explicitly lists `hw help`, `hw version`, `hw status`, `hw detectreader`, `hw tune`, `hw reset`, and other hardware commands. urlRfidResearchGroup/proxmark3 command referencehttps://github.com/RfidResearchGroup/proxmark3/blob/master/doc/commands.md

### First commands normally useful for hardware identification

```text
help
hw help
hw version
hw status
hw tune
```

`hw version` is the classic PM3-side command for displaying version information about the connected Proxmark. `hw status` is available in the current RRG command reference for runtime status. urlRfidResearchGroup/proxmark3 commands.mdhttps://github.com/RfidResearchGroup/proxmark3/blob/master/doc/commands.md

Some older command references instead show a smaller `hw` set, including `hw version`, `hw reset`, `hw tune`, `hw readmem`, `hw fpgaoff`, and `hw detectreader`. This is one reason the client must not assume that a command list from one PM3 revision is universal. urlProxmark3 historical command referencehttps://github.com/Proxmark/proxmark3/wiki/commands

## Important: `hw info` vs `hw version`

Do **not** document `hw info` as a confirmed PM5 command merely because another Proxmark variant or another AI suggested it.

For the reference PM3 command set, the documented identification command is `hw version`; the current RRG command reference also contains `hw status`. The exact PM5 command set remains a hardware/firmware verification task until we connect the device. urlRfidResearchGroup/proxmark3 command referencehttps://github.com/RfidResearchGroup/proxmark3/blob/master/doc/commands.md

The Control Center should therefore implement its own semantic operation:

```text
PM5 Inspector → Identify device
```

Internally, it may use whichever verified PM5 transport/protocol operation is appropriate. The user should not have to know whether that operation corresponds to `hw version`, a PM5-specific command, a BWM request, or several protocol probes.

## PM5-specific situation

The public Proxmark5 BWM firmware repository documents the ESP32-C2 wireless/power subsystem and its separate command namespace. It explicitly states that Bluetooth-related command codes begin at `4000` and warns that the order of existing commands must not be changed because that can break compatibility with older firmware. urlProxmark5 BWM ESP32 firmware repositoryhttps://github.com/RfidResearchGroup/Proxmark5_BWM_esp32

This is **not** the same thing as saying that a user should type a command such as `4000` into the PM3 CLI. BWM command IDs belong to the BWM protocol layer.

The BWM hardware documentation identifies an ESP32-C2-based module with Wi-Fi/Bluetooth functionality, battery fuel-gauge and charging components, and UART/I2C interfaces to the PM5 host. urlProxmark5 BWM hardware repositoryhttps://github.com/RfidResearchGroup/Proxmark5_BWM_esp32

## What we expect to determine during first PM5 connection

The first connection is read-only. We will establish, with evidence:

```text
USB enumeration
  ↓
VID/PID + Windows device identity
  ↓
Transport type / COM interface(s)
  ↓
PM5 family + hardware revision
  ↓
ARM firmware
  ↓
FPGA information
  ↓
ESP32/BWM firmware and capabilities
  ↓
PM5-specific command/protocol surface
```

Only after those observations will a PM5 command be labelled `HARDWARE VERIFIED`.

## Command support states in the Control Center

Every discovered command or capability should ultimately be classified as one of:

| State | Meaning |
|---|---|
| `PM3_REFERENCE` | Documented in the PM3/reference software; not yet proven on PM5. |
| `PM5_PROTOCOL_VERIFIED` | Confirmed from PM5 firmware/protocol source. |
| `HARDWARE_VERIFIED` | Observed and successfully exercised on the connected physical PM5. |
| `SUPPORTED_REPORTED` | Firmware reports support/capability. |
| `EXPECTED` | Compatibility database predicts support. |
| `UNKNOWN` | Evidence is insufficient. |

The application must never turn `PM3_REFERENCE` or `EXPECTED` into `HARDWARE_VERIFIED` automatically.

## Examples of PM3 functionality that must remain separately classified

The PM3 command reference contains many RFID operations, for example:

```text
lf help
lf read
lf search
hf help
hf search
hf 14a help
hf 14a reader
hf 14a snoop
hf 14b help
hf 15 help
```

These commands demonstrate the breadth of the established PM3 client, but their presence in PM3 documentation does not prove that an identical PM5 firmware exposes the same command path. urlRfidResearchGroup/proxmark3 command referencehttps://github.com/RfidResearchGroup/proxmark3/blob/master/doc/commands.md

## Firmware-selection relevance

The Control Center must not select firmware from a command name alone. Instead it should combine:

```text
Observed hardware identity
+ observed firmware/subsystem versions
+ protocol capabilities
+ compatibility database
+ exact upstream source/commit
= human-readable firmware recommendation
```

For example:

> **PM3 reference command detected in software:** `hw version`
>
> This proves only that the PM3 client knows the command. It does not prove that the connected PM5 uses the same command path.
>
> **PM5 hardware:** detected
>
> **PM5 command support:** unknown until verified
>
> **Firmware recommendation:** do not offer an unverified image.

## Upstream projects and terminology

### RfidResearchGroup (RRG)

RfidResearchGroup maintains the widely used `proxmark3` software/firmware tree and related RFID projects. Its `proxmark3` repository is an important reference for the established PM3 ecosystem, but it is not automatically the definition of PM5 hardware compatibility. urlRfidResearchGroup on GitHubhttps://github.com/RfidResearchGroup

### Iceman

“Iceman” commonly refers to the Proxmark3 firmware/client lineage maintained by the Iceman community and associated with the RfidResearchGroup fork. The RRG repository itself describes `proxmark3` as an **Iceman Fork - Proxmark3**. urlRfidResearchGroup/proxmark3https://github.com/RfidResearchGroup/proxmark3

There are also repositories using the Iceman name/lineage for Proxmark5-related development. Those must be treated as separate evidence sources and checked by exact repository, branch/tag and commit.

### Proxmark5 BWM firmware

The public `Proxmark5_BWM_esp32` repository contains firmware for the PM5 BLE/Wi-Fi module and documents its ESP32-C2, battery-management and host interfaces. It is a PM5-specific source and should be tracked separately from the legacy PM3 client tree. urlProxmark5_BWM_esp32https://github.com/RfidResearchGroup/Proxmark5_BWM_esp32

## First-session rule

Until the physical PM5 is connected:

- `hw version` = **PM3/reference knowledge**;
- PM5 equivalent = **UNKNOWN**;
- BWM command IDs = **protocol-level information**, not CLI commands;
- exact PM5 CLI command list = **UNKNOWN**;
- firmware choice = **not yet actionable**.

After the first read-only hardware session, this document should be updated with the exact observed PM5 command surface, source commit, firmware versions, transport details and evidence level.
