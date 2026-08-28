# Upstream audit — 2026-08-29

## Scope

Compared the current PM5 Control Center direction with current upstream activity in `RfidResearchGroup/proxmark3` and the NielDK BWM work.

The Control Center is a Windows/.NET client, so firmware-only changes are not copied into the application. Only protocol-relevant or client-relevant information is adapted.

## Relevant upstream findings

### PM5 BWM hardware support

NielDK added PM5 BWM charger and RGB indicator support in commit `2535307642dc0df6c2ae8ac9b97619e30a225c06`.

The firmware change identifies:

- AW32001 BWM charger
- BQ27427 fuel gauge
- PM5 antenna RGB power/battery indicator
- PM5 buzzer integration

These are hardware/firmware capabilities. The Control Center therefore records them as capability metadata rather than attempting to control them automatically.

### BWM Wi-Fi work

The current upstream tree also contains a sequence of BWM Wi-Fi forwarding/configuration changes, including the `CMD_PM5_BWM_WIFI` command and refactoring of BWM Wi-Fi handling.

Our existing BWM command model already separates the 2000+ Wi-Fi command family from PM3 CLI commands. No write/configuration operation is enabled merely because upstream added support.

### Runtime command vocabulary

RRG added runtime command-tree based tab completion in commit `d852118de53f75c2dab786b605b61548dcb16b70`. This is primarily a native PM3 CLI feature. It is not copied into the Windows client because the Control Center uses typed operations and a restricted diagnostic surface rather than exposing the entire CLI.

## Implemented in this repository

Added `BwmHardwareFeature.cs` containing a source-derived PM5/BWM hardware feature catalog.

The catalog covers:

- charger + fuel gauge
- RGB power/battery indicator
- buzzer
- Wi-Fi forwarding/configuration
- TCP/UDP networking
- MQTT
- Bluetooth
- passthrough

It also records AW32001 and BQ27427 and explicitly marks the evidence as source/protocol verified but hardware unverified.

## Safety boundary

No upstream firmware code was transplanted into the Windows application.

No automatic command was added for:

- charger control
- RGB control
- buzzer activation
- reboot/power-off
- OTA
- Wi-Fi configuration
- network service start/stop
- passthrough

Those remain explicit future operations and require a hardware/protocol verification step first.

## Next implementation target

Once physical PM5/BWM hardware is available, the catalog can be connected to the existing read-only diagnostic result so the UI can distinguish:

1. capability known from upstream source;
2. capability advertised/reported by the connected device;
3. capability actually hardware-verified by a safe diagnostic exchange.
