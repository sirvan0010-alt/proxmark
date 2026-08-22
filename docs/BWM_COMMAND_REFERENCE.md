# Proxmark5 BWM Command Reference

## Evidence and scope

This document records the command surface documented by the PM5 Battery Wireless Module firmware source.

**Source:** `RfidResearchGroup/Proxmark5_BWM_esp32`

**Source commit:** `b918166128e05455c2dcb4e232216d453bbf29ee`

**Primary sources:** `main/app_com_defs.h`, `DEV.md`, and the corresponding handlers in `main/main.c`.

**Evidence level:** `PROTOCOL VERIFIED (source code)`.

This is **not yet `HARDWARE VERIFIED`**. The user's physical PM5 has not yet been queried by this client.

## Important distinction

These are **BWM protocol command IDs**, not necessarily commands typed into the normal PM3 interactive CLI.

The PM5 BWM module uses a binary UART protocol:

```text
Host / PM5 host
      |
      | binary command frame
      v
ESP32-C2 BWM
```

The documented frame contains header bytes, a 16-bit little-endian command/type, payload length, payload and CRC16-CCITT. Successful requests return a response with the original command ID; command failures are reported through the asynchronous command-error broadcast.

## Command families

The source defines the following major ranges:

| Range | Family | Purpose |
|---:|---|---|
| 1000+ | System/general | Version, model, heap, time, MAC, UART, NVS, logs, readiness |
| 1800+ | OTA/reboot | Firmware update and reboot |
| 2000+ | Wi-Fi mode/config | Disable/forward/scan and Wi-Fi configuration |
| 2200+ | TCP server | Server status/start/stop/configuration |
| 2300+ | TCP client | Client status/start/stop/configuration |
| 2400+ | UDP server | Server status/start/stop/configuration |
| 2500+ | UDP client | Client status/start/stop/configuration |
| 2600+ | MQTT client | Client status/start/stop/configuration |
| 4000+ | Bluetooth | Bluetooth-related commands |
| 5000+ | Passthrough | Forward host data to wireless channels |

The upstream BWM README explicitly states that Bluetooth-related command codes begin at 4000 and that existing command order must not be changed because host compatibility can depend on it.

## Read-only identification commands

These are the commands we should use as the first BWM diagnostic probes once the physical device is connected.

| Code | Symbol | Operation | Read-only? | Result |
|---:|---|---|---|---|
| 1000 | `APP_CMD_GET_VERSION_INFO` | Firmware version | YES | Version string |
| 1001 | `APP_CMD_GET_DEVICE_MODEL` | Device model | YES | 2-byte model ID; source documents `0xDA10` |
| 1002 | `APP_CMD_GET_SYS_FREE_HEAP` | Free heap | YES | `uint32_t` |
| 1003 | `APP_CMD_GET_SYS_TIMESTAMP` | System UTC timestamp | YES | Unix timestamp |
| 1004 | `APP_CMD_GET_APP_COMPILE_DATETIME` | Firmware build date/time | YES | String |
| 1006 | `APP_CMD_GET_SYS_TIME_ZONE` | Time zone | YES | String |
| 1008 | `APP_CMD_GET_SYS_BASE_MAC_ADDR` | Factory base MAC | YES | 6-byte MAC, or chip-dependent 8-byte form |
| 1009 | `APP_CMD_GET_SYS_UART_CMD_BAUD_RATE` | Current UART baud | YES | `uint32_t` |
| 1010 | `APP_CMD_GET_SYS_UART_CMD_MAX_BAUD_RATE` | Maximum UART baud | YES | `uint32_t` |
| 1012 | `APP_CMD_GET_SYS_NVS_STATS` | NVS statistics | YES | Packed 20-byte statistics structure |
| 1015 | `APP_CMD_GET_LOG_UART_FORWARD_ENABLE` | Log forwarding state | YES | `uint8_t` |
| 1017 | `APP_CMD_GET_LOG_LEVEL` | Log level | YES | `uint8_t` |
| 1018 | `APP_CMD_GET_SYS_READY_STATUS` | System readiness | YES | `0` / `1` |

### Recommended first BWM probe order

```text
1001  GET_DEVICE_MODEL
1000  GET_VERSION_INFO
1018  GET_SYS_READY_STATUS
1004  GET_APP_COMPILE_DATETIME
1008  GET_SYS_BASE_MAC_ADDR
1002  GET_SYS_FREE_HEAP
1009  GET_SYS_UART_CMD_BAUD_RATE
1010  GET_SYS_UART_CMD_MAX_BAUD_RATE
1012  GET_SYS_NVS_STATS
```

The exact order used by the application may change after real-hardware observation. The important rule is that the first session remains read-only.

## Wi-Fi command family

The documented Wi-Fi commands include:

```text
2000 SET_TO_WIFI_DISABLE_MODE
2001 SET_TO_WIFI_FORWARD_MODE
2002 SET_TO_WIFI_SCAN_MODE
2003 START_WIFI_SCAN_TASK
2004 STOP_WIFI_SCAN_TASK
2005 SET_WIFI_SCAN_CONFIG
2006 GET_WIFI_SCAN_STATUS
2007 SET_WIFI_CFG_COUNTRY
2008 GET_WIFI_CFG_COUNTRY
2009 SET_WIFI_CFG_TX_PWR
2010 GET_WIFI_CFG_TX_PWR
2011 SET_WIFI_CFG_INACTIVE_TIME
2012 GET_WIFI_CFG_INACTIVE_TIME
2013 SET_WIFI_CFG_DHCP
2014 GET_WIFI_CFG_DHCP
2015 SET_WIFI_CFG_PROTOCOL
2016 GET_WIFI_CFG_PROTOCOL
2017 SET_WIFI_CFG_MAC_ADDR
2018 GET_WIFI_CFG_MAC_ADDR
2019 SET_WIFI_CFG_IP_ADDR
2020 GET_WIFI_CFG_IP_ADDR
2021 SET_WIFI_CFG_HOST_NAME
2022 GET_WIFI_CFG_HOST_NAME
2023 SET_WIFI_CONNECT_CFG_SSID
2024 GET_WIFI_CONNECT_CFG_SSID
2025 SET_WIFI_CONNECT_CFG_PASSWORD
2026 GET_WIFI_CONNECT_CFG_PASSWORD
2027 SET_WIFI_CONNECT_CFG_BSSID
2028 GET_WIFI_CONNECT_CFG_BSSID
2029 SET_WIFI_CONNECT_CFG_AUTHMODE
2030 GET_WIFI_CONNECT_CFG_AUTHMODE
2031 SET_WIFI_CONNECT_CFG_LISTEN_INTERVAL
2032 GET_WIFI_CONNECT_CFG_LISTEN_INTERVAL
2033 SET_WIFI_CONNECT_CFG_SCAN_MODE
2034 GET_WIFI_CONNECT_CFG_SCAN_MODE
2035 SET_WIFI_CONNECT_CFG_PMF
2036 GET_WIFI_CONNECT_CFG_PMF
2037 SET_WIFI_CONNECT_CFG_RECONNECT_INTERVAL
2038 GET_WIFI_CONNECT_CFG_RECONNECT_INTERVAL
2039 SET_WIFI_SNTP_ENABLE
2040 GET_WIFI_SNTP_ENABLE
2041 SET_WIFI_SNTP_SERVER
2042 GET_WIFI_SNTP_SERVER
2043 SET_WIFI_SNTP_INTERVAL
2044 GET_WIFI_SNTP_INTERVAL
2045 START_WIFI_SNTP
2046 STOP_WIFI_SNTP
2047 GET_WIFI_SNTP_SYNC_STATUS
2048 START_WIFI_CONNECT_TASK
2049 STOP_WIFI_CONNECT_TASK
2050 GET_WIFI_CONNECT_STATUS
2051 WAIT_FOR_WIFI_CONNECT_TASK
```

**Important:** even commands beginning with `GET_` can expose configuration or network information. The client should classify their diagnostic impact separately from their write/destructive impact.

## Network command families

The source documents command ranges for TCP server/client, UDP server/client and MQTT client. Each family contains status, start/stop and configuration GET/SET operations.

```text
2200+ TCP server
2300+ TCP client
2400+ UDP server
2500+ UDP client
2600+ MQTT client
```

For the first hardware session, the Control Center should not automatically start network services or alter network configuration. These commands belong to later, explicitly requested functionality.

## Passthrough

The documented passthrough send operation is:

```text
5000 APP_CMD_SEND_FORWARD_DATA
```

It forwards host data to currently active wireless channels. Incoming wireless data is reported through broadcast type `8089` (`APP_BROADCAST_DATA_FORWARD`).

This is a transport/data-forwarding mechanism, not a normal PM3 CLI command.

## Broadcast / asynchronous reports

The source defines:

| Code | Symbol | Meaning |
|---:|---|---|
| 8088 | `APP_BROADCAST_WIFI_SCAN_RESULT` | Wi-Fi scan result |
| 8089 | `APP_BROADCAST_DATA_FORWARD` | Forwarded wireless data |
| 8090 | `APP_BROADCAST_SYS_LOG_MESSAGE` | System log message |
| 8091 | `APP_BROADCAST_CMD_ERROR` | Command execution failure |

These are unsolicited module-to-host events, not request commands.

## Commands explicitly excluded from first inspection

The following must **not** be sent automatically during the first PM5 session:

```text
SET_* configuration commands
START_* / STOP_* operational commands
OTA_BEGIN
OTA_WRITE
OTA_END
REBOOT
RESTORE_TO_FACTORY_SETTINGS
SEND_FORWARD_DATA
```

The first session is intended to identify and preserve the device state, not modify it.

## What this gives the Control Center

The application can now distinguish three different command layers:

```text
PM3 CLI commands
    hw version / hw status / ...
          |
          | NOT automatically equivalent
          v
PM5 host / protocol layer
          |
          v
BWM binary command IDs
    1000, 1001, 1002, ...
```

This is exactly why the application should expose a semantic operation such as:

```text
Connect → Diagnose → Identify BWM
```

rather than requiring the user to know whether a particular firmware implementation uses `hw version`, a PM5-specific host command, or BWM command `1000`.

## Evidence status

| Item | Status |
|---|---|
| BWM command definitions | `PROTOCOL VERIFIED` |
| BWM frame format | `PROTOCOL VERIFIED` |
| BWM CRC algorithm/scope | `PROTOCOL VERIFIED` |
| BWM command payload layouts | `PROTOCOL VERIFIED` from source documentation |
| Physical PM5 USB identity | `UNKNOWN` until connection |
| Physical PM5 BWM presence | `UNKNOWN` until connection |
| Physical PM5 BWM firmware version | `UNKNOWN` until connection |
| Physical PM5 supported command surface | `UNKNOWN` until queried |

## Source links

- BWM firmware repository: https://github.com/RfidResearchGroup/Proxmark5_BWM_esp32
- Source commit used here: `b918166128e05455c2dcb4e232216d453bbf29ee`
- `main/app_com_defs.h`: https://github.com/RfidResearchGroup/Proxmark5_BWM_esp32/blob/b918166128e05455c2dcb4e232216d453bbf29ee/main/app_com_defs.h
- `DEV.md`: https://github.com/RfidResearchGroup/Proxmark5_BWM_esp32/blob/b918166128e05455c2dcb4e232216d453bbf29ee/DEV.md
