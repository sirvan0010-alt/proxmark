// PM5 Control Center — BWM command codes
// PROVENANCE: generated from the official firmware source
// RfidResearchGroup/Proxmark5_BWM_esp32, main/app_com_defs.h
// commit b918166128e05455c2dcb4e232216d453bbf29ee (2026-08-08)
//
// DO NOT reorder or renumber these values by hand. The upstream header
// itself states new commands must be appended, never inserted, to avoid
// breaking host/firmware compatibility — this file mirrors that contract.
// If the upstream header changes, regenerate this file and bump the
// commit hash above; do not hand-edit around a stale snapshot.
namespace PM5Control.Core.Protocols.Bwm;

/// <summary>
/// Broadcast message type codes (sent as the "commandId" field on
/// BwmFrameKind.Broadcast frames).
/// </summary>
public enum BwmBroadcastType : ushort
{
    /// <summary>WiFi scan async report.</summary>
    WifiScanResult = 8088,

    /// <summary>Transparent-forward data.</summary>
    DataForward = 8089,

    /// <summary>System log message; all ESP_LOGx output is also forwarded here.</summary>
    SysLogMessage = 8090,

    /// <summary>Command execution failure report; payload is cmd(uint16) + err(int32).</summary>
    CmdError = 8091,
}

/// <summary>
/// Request/response command codes for the BWM UART protocol.
/// </summary>
public enum BwmCommandCode : ushort
{
    // ---- System and general utility commands (start at 1000) ----

    /// <summary>Get firmware version.</summary>
    GetVersionInfo = 1000,
    /// <summary>Get device model.</summary>
    GetDeviceModel,
    /// <summary>Get system free heap.</summary>
    GetSysFreeHeap,
    /// <summary>Get system time.</summary>
    GetSysTimestamp,
    /// <summary>Get current app firmware compile date-time (string).</summary>
    GetAppCompileDatetime,
    /// <summary>Set system time.</summary>
    SetSysTimestamp,
    /// <summary>Get time zone.</summary>
    GetSysTimeZone,
    /// <summary>Set time zone (persisted).</summary>
    SetSysTimeZone,
    /// <summary>Get factory-fixed base MAC address (from eFuse, read-only).</summary>
    GetSysBaseMacAddr,
    /// <summary>Get command UART current baud rate.</summary>
    GetSysUartCmdBaudRate,
    /// <summary>Get command UART maximum baud rate for this chip.</summary>
    GetSysUartCmdMaxBaudRate,
    /// <summary>Set command UART baud rate (must NOT be persisted).</summary>
    SetSysUartCmdBaudRate,
    /// <summary>Get NVS statistics.</summary>
    GetSysNvsStats,
    /// <summary>Restore factory settings; erases all user config.</summary>
    RestoreToFactorySettings,
    /// <summary>Enable log forwarding to command UART.</summary>
    SetLogUartForwardEnable,
    /// <summary>Get log-forward-to-command-UART state.</summary>
    GetLogUartForwardEnable,
    /// <summary>Set log output level.</summary>
    SetLogLevel,
    /// <summary>Get log output level.</summary>
    GetLogLevel,
    /// <summary>Get system ready status; only safe to call other commands after system is ready.</summary>
    GetSysReadyStatus,

    // NOTE (upstream): OTA and reboot commands are critical for firmware
    // download during development; do NOT change their codes (order).

    /// <summary>OTA: begin writing new firmware; param = total size (uint32).</summary>
    OtaBegin = 1800,
    /// <summary>OTA: write firmware chunk.</summary>
    OtaWrite,
    /// <summary>OTA: finish writing and set next boot partition.</summary>
    OtaEnd,
    /// <summary>Reboot the system.</summary>
    Reboot,

    // ---- WiFi-related commands (start at 2000) ----

    /// <summary>Switch to WiFi disabled mode.</summary>
    SetToWifiDisableMode = 2000,
    /// <summary>Switch to WiFi forward mode.</summary>
    SetToWifiForwardMode,
    /// <summary>Switch to WiFi scan mode.</summary>
    SetToWifiScanMode,
    /// <summary>Start WiFi scan task.</summary>
    StartWifiScanTask,
    /// <summary>Stop WiFi scan task.</summary>
    StopWifiScanTask,
    /// <summary>Set WiFi scan startup config.</summary>
    SetWifiScanConfig,
    /// <summary>Get WiFi scan status.</summary>
    GetWifiScanStatus,
    /// <summary>WiFi config: set country code (persisted).</summary>
    SetWifiCfgCountry,
    /// <summary>WiFi config: get country code.</summary>
    GetWifiCfgCountry,
    /// <summary>WiFi config: set TX power (persisted).</summary>
    SetWifiCfgTxPwr,
    /// <summary>WiFi config: get TX power.</summary>
    GetWifiCfgTxPwr,
    /// <summary>WiFi config: set inactive time.</summary>
    SetWifiCfgInactiveTime,
    /// <summary>WiFi config: get inactive time.</summary>
    GetWifiCfgInactiveTime,
    /// <summary>WiFi config: set DHCP enable.</summary>
    SetWifiCfgDhcp,
    /// <summary>WiFi config: check DHCP enable.</summary>
    GetWifiCfgDhcp,
    /// <summary>WiFi config: set WiFi protocol standard.</summary>
    SetWifiCfgProtocol,
    /// <summary>WiFi config: get WiFi protocol standard.</summary>
    GetWifiCfgProtocol,
    /// <summary>WiFi config: set WiFi MAC address.</summary>
    SetWifiCfgMacAddr,
    /// <summary>WiFi config: get WiFi MAC address.</summary>
    GetWifiCfgMacAddr,
    /// <summary>WiFi config: set WiFi IP address.</summary>
    SetWifiCfgIpAddr,
    /// <summary>WiFi config: get WiFi IP address.</summary>
    GetWifiCfgIpAddr,
    /// <summary>WiFi config: set WiFi hostname.</summary>
    SetWifiCfgHostName,
    /// <summary>WiFi config: get WiFi hostname.</summary>
    GetWifiCfgHostName,
    /// <summary>WiFi config: set target SSID.</summary>
    SetWifiConnectCfgSsid,
    /// <summary>WiFi config: get target SSID.</summary>
    GetWifiConnectCfgSsid,
    /// <summary>WiFi config: set target password.</summary>
    SetWifiConnectCfgPassword,
    /// <summary>WiFi config: get target password.</summary>
    GetWifiConnectCfgPassword,
    /// <summary>WiFi config: set target BSSID.</summary>
    SetWifiConnectCfgBssid,
    /// <summary>WiFi config: get target BSSID.</summary>
    GetWifiConnectCfgBssid,
    /// <summary>WiFi config: set auth mode threshold.</summary>
    SetWifiConnectCfgAuthmode,
    /// <summary>WiFi config: get auth mode threshold.</summary>
    GetWifiConnectCfgAuthmode,
    /// <summary>WiFi config: set AP beacon listen interval.</summary>
    SetWifiConnectCfgListenInterval,
    /// <summary>WiFi config: get AP beacon listen interval.</summary>
    GetWifiConnectCfgListenInterval,
    /// <summary>WiFi config: set scan mode.</summary>
    SetWifiConnectCfgScanMode,
    /// <summary>WiFi config: get scan mode.</summary>
    GetWifiConnectCfgScanMode,
    /// <summary>WiFi config: set PMF (Protected Management Frames).</summary>
    SetWifiConnectCfgPmf,
    /// <summary>WiFi config: get PMF (Protected Management Frames).</summary>
    GetWifiConnectCfgPmf,
    /// <summary>WiFi config: set reconnect interval.</summary>
    SetWifiConnectCfgReconnectInterval,
    /// <summary>WiFi config: get reconnect interval.</summary>
    GetWifiConnectCfgReconnectInterval,
    /// <summary>WiFi config: set SNTP enable.</summary>
    SetWifiSntpEnable,
    /// <summary>WiFi config: get SNTP enable.</summary>
    GetWifiSntpEnable,
    /// <summary>WiFi config: set SNTP server address.</summary>
    SetWifiSntpServer,
    /// <summary>WiFi config: get SNTP server address.</summary>
    GetWifiSntpServer,
    /// <summary>WiFi config: set SNTP sync interval.</summary>
    SetWifiSntpInterval,
    /// <summary>WiFi config: get SNTP sync interval.</summary>
    GetWifiSntpInterval,
    /// <summary>WiFi control: start SNTP.</summary>
    StartWifiSntp,
    /// <summary>WiFi control: stop SNTP.</summary>
    StopWifiSntp,
    /// <summary>WiFi config: get SNTP sync status.</summary>
    GetWifiSntpSyncStatus,
    /// <summary>Start WiFi connection task.</summary>
    StartWifiConnectTask,
    /// <summary>Stop WiFi connection task; disconnects any existing connection.</summary>
    StopWifiConnectTask,
    /// <summary>Get WiFi connection task status.</summary>
    GetWifiConnectStatus,
    /// <summary>Wait for WiFi connection task to succeed, fail, or timeout.</summary>
    WaitForWifiConnectTask,

    // ---- TCP server commands (start at 2200) ----

    /// <summary>TCP server: get status.</summary>
    GetTcpServerStatus = 2200,
    /// <summary>TCP server: start.</summary>
    StartTcpServer,
    /// <summary>TCP server: stop.</summary>
    StopTcpServer,
    /// <summary>TCP server config: set IP protocol.</summary>
    SetTcpServerIpProtocol,
    /// <summary>TCP server config: get IP protocol.</summary>
    GetTcpServerIpProtocol,
    /// <summary>TCP server config: set listen port.</summary>
    SetTcpServerPort,
    /// <summary>TCP server config: get listen port.</summary>
    GetTcpServerPort,
    /// <summary>TCP server config: set SO_LINGER.</summary>
    SetTcpServerSoLinger,
    /// <summary>TCP server config: get SO_LINGER.</summary>
    GetTcpServerSoLinger,
    /// <summary>TCP server config: set TCP_NODELAY.</summary>
    SetTcpServerNodelay,
    /// <summary>TCP server config: get TCP_NODELAY.</summary>
    GetTcpServerNodelay,
    /// <summary>TCP server config: set SO_SNDTIMEO.</summary>
    SetTcpServerSoSndtimeo,
    /// <summary>TCP server config: get SO_SNDTIMEO.</summary>
    GetTcpServerSoSndtimeo,
    /// <summary>TCP server config: set keep-alive.</summary>
    SetTcpServerKeepAlive,
    /// <summary>TCP server config: get keep-alive.</summary>
    GetTcpServerKeepAlive,

    // ---- TCP client commands (start at 2300) ----

    /// <summary>TCP client: get status.</summary>
    GetTcpClientStatus = 2300,
    /// <summary>TCP client: start.</summary>
    StartTcpClient,
    /// <summary>TCP client: stop.</summary>
    StopTcpClient,
    /// <summary>TCP client config: set IP address.</summary>
    SetTcpClientIpAddr,
    /// <summary>TCP client config: get IP address.</summary>
    GetTcpClientIpAddr,
    /// <summary>TCP client config: set port.</summary>
    SetTcpClientPort,
    /// <summary>TCP client config: get port.</summary>
    GetTcpClientPort,
    /// <summary>TCP client config: set SO_LINGER.</summary>
    SetTcpClientSoLinger,
    /// <summary>TCP client config: get SO_LINGER.</summary>
    GetTcpClientSoLinger,
    /// <summary>TCP client config: set TCP_NODELAY.</summary>
    SetTcpClientNodelay,
    /// <summary>TCP client config: get TCP_NODELAY.</summary>
    GetTcpClientNodelay,
    /// <summary>TCP client config: set SO_SNDTIMEO.</summary>
    SetTcpClientSoSndtimeo,
    /// <summary>TCP client config: get SO_SNDTIMEO.</summary>
    GetTcpClientSoSndtimeo,
    /// <summary>TCP client config: set keep-alive.</summary>
    SetTcpClientKeepAlive,
    /// <summary>TCP client config: get keep-alive.</summary>
    GetTcpClientKeepAlive,

    // ---- UDP server commands (start at 2400) ----

    /// <summary>UDP server: get status.</summary>
    GetUdpServerStatus = 2400,
    /// <summary>UDP server: start.</summary>
    StartUdpServer,
    /// <summary>UDP server: stop.</summary>
    StopUdpServer,
    /// <summary>UDP server config: set IP protocol.</summary>
    SetUdpServerIpProtocol,
    /// <summary>UDP server config: get IP protocol.</summary>
    GetUdpServerIpProtocol,
    /// <summary>UDP server config: set listen port.</summary>
    SetUdpServerPort,
    /// <summary>UDP server config: get listen port.</summary>
    GetUdpServerPort,
    /// <summary>UDP server config: set SO_SNDTIMEO.</summary>
    SetUdpServerSoSndtimeo,
    /// <summary>UDP server config: get SO_SNDTIMEO.</summary>
    GetUdpServerSoSndtimeo,
    /// <summary>UDP server config: set fixed target IP address.</summary>
    SetUdpServerClientIpAddr,
    /// <summary>UDP server config: get fixed target IP address.</summary>
    GetUdpServerClientIpAddr,
    /// <summary>UDP server config: set fixed target port.</summary>
    SetUdpServerClientPort,
    /// <summary>UDP server config: get fixed target port.</summary>
    GetUdpServerClientPort,

    // ---- UDP client commands (start at 2500) ----

    /// <summary>UDP client: get status.</summary>
    GetUdpClientStatus = 2500,
    /// <summary>UDP client: start.</summary>
    StartUdpClient,
    /// <summary>UDP client: stop.</summary>
    StopUdpClient,
    /// <summary>UDP client config: set IP protocol.</summary>
    SetUdpClientIpProtocol,
    /// <summary>UDP client config: get IP protocol.</summary>
    GetUdpClientIpProtocol,
    /// <summary>UDP client config: set local port.</summary>
    SetUdpClientLocalPort,
    /// <summary>UDP client config: get local port.</summary>
    GetUdpClientLocalPort,
    /// <summary>UDP client config: set SO_SNDTIMEO.</summary>
    SetUdpClientSoSndtimeo,
    /// <summary>UDP client config: get SO_SNDTIMEO.</summary>
    GetUdpClientSoSndtimeo,
    /// <summary>UDP client config: set target server IP address.</summary>
    SetUdpClientServerIpAddr,
    /// <summary>UDP client config: get target server IP address.</summary>
    GetUdpClientServerIpAddr,
    /// <summary>UDP client config: set target server port.</summary>
    SetUdpClientServerPort,
    /// <summary>UDP client config: get target server port.</summary>
    GetUdpClientServerPort,

    // ---- MQTT client commands (start at 2600) ----

    /// <summary>MQTT client: get status.</summary>
    GetMqttClientStatus = 2600,
    /// <summary>MQTT client: start.</summary>
    StartMqttClient,
    /// <summary>MQTT client: stop.</summary>
    StopMqttClient,
    /// <summary>MQTT client config: set broker host address.</summary>
    SetMqttClientHost,
    /// <summary>MQTT client config: get broker host address.</summary>
    GetMqttClientHost,
    /// <summary>MQTT client config: set broker port.</summary>
    SetMqttClientPort,
    /// <summary>MQTT client config: get broker port.</summary>
    GetMqttClientPort,
    /// <summary>MQTT client config: set broker path.</summary>
    SetMqttClientPath,
    /// <summary>MQTT client config: get broker path.</summary>
    GetMqttClientPath,
    /// <summary>MQTT client config: set connection scheme.</summary>
    SetMqttClientScheme,
    /// <summary>MQTT client config: get connection scheme.</summary>
    GetMqttClientScheme,
    /// <summary>MQTT client config: set subscribe topic.</summary>
    SetMqttClientSubscribeTopic,
    /// <summary>MQTT client config: get subscribe topic.</summary>
    GetMqttClientSubscribeTopic,
    /// <summary>MQTT client config: set subscribe QoS.</summary>
    SetMqttClientSubscribeQos,
    /// <summary>MQTT client config: get subscribe QoS.</summary>
    GetMqttClientSubscribeQos,
    /// <summary>MQTT client config: set publish topic.</summary>
    SetMqttClientPublishTopic,
    /// <summary>MQTT client config: get publish topic.</summary>
    GetMqttClientPublishTopic,
    /// <summary>MQTT client config: set publish QoS.</summary>
    SetMqttClientPublishQos,
    /// <summary>MQTT client config: get publish QoS.</summary>
    GetMqttClientPublishQos,
    /// <summary>MQTT client config: set publish retain flag.</summary>
    SetMqttClientPublishRetain,
    /// <summary>MQTT client config: get publish retain flag.</summary>
    GetMqttClientPublishRetain,
    /// <summary>MQTT client config: set client ID.</summary>
    SetMqttClientClientId,
    /// <summary>MQTT client config: get client ID.</summary>
    GetMqttClientClientId,
    /// <summary>MQTT client config: set username.</summary>
    SetMqttClientUsername,
    /// <summary>MQTT client config: get username.</summary>
    GetMqttClientUsername,
    /// <summary>MQTT client config: set password.</summary>
    SetMqttClientPassword,
    /// <summary>MQTT client config: get password.</summary>
    GetMqttClientPassword,
    /// <summary>MQTT client config: set keep-alive.</summary>
    SetMqttClientKeepAlive,
    /// <summary>MQTT client config: get keep-alive.</summary>
    GetMqttClientKeepAlive,
    /// <summary>MQTT client config: set disable clean session flag.</summary>
    SetMqttClientDisableCleanSession,
    /// <summary>MQTT client config: get disable clean session flag.</summary>
    GetMqttClientDisableCleanSession,
    /// <summary>MQTT client config: set LWT topic.</summary>
    SetMqttClientLwtTopic,
    /// <summary>MQTT client config: get LWT topic.</summary>
    GetMqttClientLwtTopic,
    /// <summary>MQTT client config: set LWT message.</summary>
    SetMqttClientLwtMessage,
    /// <summary>MQTT client config: get LWT message.</summary>
    GetMqttClientLwtMessage,
    /// <summary>MQTT client config: set LWT QoS.</summary>
    SetMqttClientLwtQos,
    /// <summary>MQTT client config: get LWT QoS.</summary>
    GetMqttClientLwtQos,
    /// <summary>MQTT client config: set LWT retain flag.</summary>
    SetMqttClientLwtRetain,
    /// <summary>MQTT client config: get LWT retain flag.</summary>
    GetMqttClientLwtRetain,
    /// <summary>MQTT client config: add ALPN protocol.</summary>
    AddMqttClientAlpn,
    /// <summary>MQTT client config: remove ALPN protocol.</summary>
    DelMqttClientAlpn,
    /// <summary>MQTT client config: get ALPN protocol list.</summary>
    GetMqttClientAlpn,
    /// <summary>MQTT client config: get ALPN protocol count.</summary>
    GetMqttClientAlpnCount,
    /// <summary>MQTT client config: clear ALPN protocol list.</summary>
    ClearMqttClientAlpn,
    /// <summary>MQTT client config: set SNI server name.</summary>
    SetMqttClientSniHost,
    /// <summary>MQTT client config: get SNI server name.</summary>
    GetMqttClientSniHost,
    /// <summary>MQTT client config: set server CA certificate.</summary>
    SetMqttClientCacert,
    /// <summary>MQTT client config: get server CA certificate.</summary>
    GetMqttClientCacert,
    /// <summary>MQTT client config: set client certificate.</summary>
    SetMqttClientCcert,
    /// <summary>MQTT client config: get client certificate.</summary>
    GetMqttClientCcert,
    /// <summary>MQTT client config: set client private key.</summary>
    SetMqttClientCckey,
    /// <summary>MQTT client config: get client private key.</summary>
    GetMqttClientCckey,
    /// <summary>MQTT client config: set PSK key data.</summary>
    SetMqttClientPskData,
    /// <summary>MQTT client config: get PSK key data.</summary>
    GetMqttClientPskData,
    /// <summary>MQTT client config: set PSK key identity hint.</summary>
    SetMqttClientPskHint,
    /// <summary>MQTT client config: get PSK key identity hint.</summary>
    GetMqttClientPskHint,

    // ---- BLE commands (start at 4000) ----

    /// <summary>BLE config: set advertising manufacturer data.</summary>
    SetBleAdvMfgData = 4000,
    /// <summary>BLE config: get advertising manufacturer data.</summary>
    GetBleAdvMfgData,
    /// <summary>BLE config: set device name.</summary>
    SetBleDeviceName,
    /// <summary>BLE config: get device name.</summary>
    GetBleDeviceName,
    /// <summary>BLE config: set notify retry max count.</summary>
    SetBleNotifyRetryMax,
    /// <summary>BLE config: get notify retry max count.</summary>
    GetBleNotifyRetryMax,
    /// <summary>BLE config: set device address.</summary>
    SetBleDeviceAddr,
    /// <summary>BLE config: get device address.</summary>
    GetBleDeviceAddr,
    /// <summary>BLE config: set bonding enable.</summary>
    SetBleBondingEnable,
    /// <summary>BLE config: get bonding enable state.</summary>
    GetBleBondingEnable,
    /// <summary>BLE config: set bonding passkey (6-digit string).</summary>
    SetBleBondingKey,
    /// <summary>BLE config: get bonding passkey.</summary>
    GetBleBondingKey,
    /// <summary>BLE config: get number of bonded devices.</summary>
    GetBleBondedDeviceNums,
    /// <summary>BLE config: get bonded device address.</summary>
    GetBleBondedDeviceAddr,
    /// <summary>BLE config: delete specified bonded device.</summary>
    DelBleBondedDevice,
    /// <summary>BLE config: clear all bonded devices.</summary>
    ClearBleBonded,
    /// <summary>BLE config: set battery level.</summary>
    SetBleBatteryLevel,
    /// <summary>BLE config: get battery level.</summary>
    GetBleBatteryLevel,
    /// <summary>BLE config: set TX power level.</summary>
    SetBleTxPower,
    /// <summary>BLE config: get TX power level.</summary>
    GetBleTxPower,
    /// <summary>BLE control: get BLE SPP service status.</summary>
    GetBleSppStatus,
    /// <summary>BLE control: start BLE SPP service.</summary>
    StartBleSpp,
    /// <summary>BLE control: stop BLE SPP service.</summary>
    StopBleSpp,

    // ---- Other general commands (start at 5000) ----

    /// <summary>Send forward data; destination (BLE or WiFi) depends on current forward mode config.</summary>
    SendForwardData = 5000,
}
