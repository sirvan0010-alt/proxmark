# Real PM5 Hardware Baseline

## Status

**WAITING FOR PHYSICAL DEVICE CONNECTION**

This document defines the first inspection of the user's actual Proxmark5. The first session is read-only.

## Objective

Create an immutable software record of what the physical device actually reports before any firmware update, configuration change or experimental operation.

## Pre-flight

1. Use a known-good USB-C data cable.
2. Connect the PM5 directly to the Windows PC for the first inspection; avoid hubs where practical.
3. Do not install or replace drivers automatically.
4. Record the Windows USB device entry and VID/PID.
5. If Windows does not expose the expected interface, stop and investigate the transport requirement before changing drivers.
6. Zadig is not a default prerequisite. Use it only after the exact required driver/interface is established.
7. Do not flash firmware.
8. Do not erase, format, repartition or write configuration.
9. Do not change BWM/Wi-Fi/BLE settings.

## Read-only inspection sequence

```text
USB enumeration
    ↓
Transport identification
    ↓
PM5 hardware identification
    ↓
ARM firmware identification
    ↓
FPGA identification
    ↓
BWM/ESP32 identification
    ↓
BWM system information
    ↓
Wi-Fi/BLE capability/status
    ↓
Power/battery telemetry
    ↓
Compatibility comparison
    ↓
Baseline report
```

## Information to capture

### USB
- VID
- PID
- USB descriptors where available
- Windows device/interface name
- driver/provider/version
- assigned COM/interface information if applicable

### PM5 main system
- hardware model
- hardware revision
- serial/device ID if exposed
- ARM firmware version/build
- FPGA version/build
- reported memory
- capabilities

### BWM/ESP32
- MCU/model
- BWM firmware version/build
- compile date/time
- MAC address
- free heap
- uptime/timestamp if exposed
- UART settings
- ready state
- NVS/system statistics

### Wireless
- Wi-Fi support/status
- BLE support/status
- TCP/UDP capability
- MQTT capability
- actual network configuration only if read-only inspection exposes it

### Power
- external power state
- battery presence
- fuel-gauge information
- charging state
- voltage/current/temperature/percentage only when actually exposed

## Evidence requirements

Every captured value should include:

- source (`DETECTED`, `REPORTED`, `EXPECTED`, `UNKNOWN`)
- exact value
- timestamp
- firmware/version context
- protocol/source where applicable

## Baseline artifacts

The inspector should eventually export:

```text
baseline/
  device.json
  diagnostics.json
  compatibility.json
  usb.json
  bwm.json
  power.json
  environment.json
  README.md
```

Do not store secrets such as Wi-Fi passwords or private keys in the baseline.

## Stop conditions

Stop and ask for review if:

- a command appears to modify state;
- firmware writing is requested;
- a backup/extraction method is uncertain;
- the observed hardware differs materially from the expected PM5 architecture;
- the USB driver requirement is unclear;
- a tool proposes changing device state merely to identify the hardware.

## Completion criterion

The baseline is complete when the physical PM5 has a reproducible read-only report and no device state has been intentionally modified.
