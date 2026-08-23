// PM5 Control Center — BWM read-only adapter v2.1
// PURPOSE: execute the explicitly reviewed first-session BWM inspector
// commands and decode only payload shapes documented by the verified
// upstream command reference. Unknown/unsupported shapes remain raw or
// UNKNOWN rather than being guessed.
// PROVENANCE: RfidResearchGroup/Proxmark5_BWM_esp32,
// b918166128e05455c2dcb4e232216d453bbf29ee.
// SAFETY: command authorization is centralized in BwmReadOnlyPolicy.
// No mutating command reaches the transport through this adapter.

using System.Buffers.Binary;
using System.Text;
using PM5Control.Core.Connections;
using PM5Control.Core.Devices;
using PM5Control.Core.Diagnostics;

namespace PM5Control.Core.Protocols.Bwm;

public sealed class BwmReadOnlyAdapter : IProxmarkProtocol
{
    private readonly IProxmarkTransport _transport;

    public BwmReadOnlyAdapter(IProxmarkTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public string ProtocolName => "BWM";

    public static IReadOnlySet<BwmCommandCode> AllowedCommands => BwmReadOnlyPolicy.AllowedCommands;

    public async Task<BwmFrame?> QueryAsync(
        BwmCommandCode command,
        ReadOnlyMemory<byte> payload = default,
        CancellationToken cancellationToken = default)
    {
        if (!BwmReadOnlyPolicy.IsAllowed(command))
            return null;

        var request = BwmFrameCodec.EncodeRequest((ushort)command, payload.Span);
        byte[] responseBytes;
        try
        {
            responseBytes = await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }

        if (!BwmFrameCodec.TryDecode(responseBytes, out var frame) || frame is null)
            return null;

        if (frame.Kind != BwmFrameKind.Response || frame.CommandId != (ushort)command)
            return null;

        return frame;
    }

    public async Task<DiagnosticValue<byte[]>> QueryPayloadAsync(
        BwmCommandCode command,
        ReadOnlyMemory<byte> payload = default,
        CancellationToken cancellationToken = default)
    {
        var frame = await QueryAsync(command, payload, cancellationToken).ConfigureAwait(false);
        if (frame is null)
        {
            return new DiagnosticValue<byte[]>(
                null,
                DiagnosticSourceState.Unknown,
                DiagnosticConfidence.Unknown,
                $"{command} — no valid response",
                DateTimeOffset.UtcNow);
        }

        return new DiagnosticValue<byte[]>(
            frame.Payload,
            DiagnosticSourceState.Reported,
            DiagnosticConfidence.Medium,
            $"{command} — payload verified at framing level; semantic layout remains source-documented and not hardware-verified",
            DateTimeOffset.UtcNow);
    }

    public Task<DiagnosticValue<string>> GetVersionInfoAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeStringAsync(BwmCommandCode.GetVersionInfo, "APP_CMD_GET_VERSION_INFO", cancellationToken);

    /// <summary>Returns the source-documented uint16 device model ID. It is not a string.</summary>
    public async Task<DiagnosticValue<ushort>> GetDeviceModelIdAsync(CancellationToken cancellationToken = default)
    {
        var frame = await QueryAsync(BwmCommandCode.GetDeviceModel, cancellationToken: cancellationToken).ConfigureAwait(false);
        return DecodeUInt16(frame, "APP_CMD_GET_DEVICE_MODEL");
    }

    public Task<DiagnosticValue<long>> GetSysFreeHeapAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeUInt32Async(BwmCommandCode.GetSysFreeHeap, "APP_CMD_GET_SYS_FREE_HEAP", cancellationToken);

    public Task<DiagnosticValue<long>> GetSysTimestampAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeUInt32Async(BwmCommandCode.GetSysTimestamp, "APP_CMD_GET_SYS_TIMESTAMP", cancellationToken);

    public Task<DiagnosticValue<string>> GetAppCompileDatetimeAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeStringAsync(BwmCommandCode.GetAppCompileDatetime, "APP_CMD_GET_APP_COMPILE_DATETIME", cancellationToken);

    public Task<DiagnosticValue<string>> GetSysTimeZoneAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeStringAsync(BwmCommandCode.GetSysTimeZone, "APP_CMD_GET_SYS_TIME_ZONE", cancellationToken);

    public async Task<DiagnosticValue<string>> GetSysBaseMacAddrAsync(CancellationToken cancellationToken = default)
    {
        var frame = await QueryAsync(BwmCommandCode.GetSysBaseMacAddr, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (frame is null || frame.Payload.Length != 6)
            return Unknown<string>("APP_CMD_GET_SYS_BASE_MAC_ADDR — expected exactly 6-byte MAC payload");

        var mac = string.Join(":", frame.Payload.Select(b => b.ToString("X2")));
        return new DiagnosticValue<string>(
            mac,
            DiagnosticSourceState.Reported,
            DiagnosticConfidence.Medium,
            "APP_CMD_GET_SYS_BASE_MAC_ADDR — source-documented 6-byte MAC; physical device not yet verified",
            DateTimeOffset.UtcNow);
    }

    public Task<DiagnosticValue<long>> GetSysUartCmdBaudRateAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeUInt32Async(BwmCommandCode.GetSysUartCmdBaudRate, "APP_CMD_GET_SYS_UART_CMD_BAUD_RATE", cancellationToken);

    public Task<DiagnosticValue<long>> GetSysUartCmdMaxBaudRateAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeUInt32Async(BwmCommandCode.GetSysUartCmdMaxBaudRate, "APP_CMD_GET_SYS_UART_CMD_MAX_BAUD_RATE", cancellationToken);

    public async Task<DiagnosticValue<byte[]>> GetSysNvsStatsAsync(CancellationToken cancellationToken = default)
    {
        var frame = await QueryAsync(BwmCommandCode.GetSysNvsStats, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (frame is null || frame.Payload.Length != 20)
        {
            return new DiagnosticValue<byte[]>(
                null,
                DiagnosticSourceState.Unknown,
                DiagnosticConfidence.Unknown,
                "APP_CMD_GET_SYS_NVS_STATS — expected source-documented packed 20-byte payload",
                DateTimeOffset.UtcNow);
        }

        return new DiagnosticValue<byte[]>(
            frame.Payload,
            DiagnosticSourceState.Reported,
            DiagnosticConfidence.Medium,
            "APP_CMD_GET_SYS_NVS_STATS — packed 20-byte payload retained raw; field semantics not guessed",
            DateTimeOffset.UtcNow);
    }

    public async Task<DiagnosticValue<bool>> GetLogUartForwardEnableAsync(CancellationToken cancellationToken = default)
    {
        var frame = await QueryAsync(BwmCommandCode.GetLogUartForwardEnable, cancellationToken: cancellationToken).ConfigureAwait(false);
        return DecodeByteBool(frame, "APP_CMD_GET_LOG_UART_FORWARD_ENABLE");
    }

    public async Task<DiagnosticValue<byte>> GetLogLevelAsync(CancellationToken cancellationToken = default)
    {
        var frame = await QueryAsync(BwmCommandCode.GetLogLevel, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (frame is null || frame.Payload.Length != 1)
            return new DiagnosticValue<byte>(0, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown, "APP_CMD_GET_LOG_LEVEL — expected 1-byte uint8", DateTimeOffset.UtcNow);

        return new DiagnosticValue<byte>(frame.Payload[0], DiagnosticSourceState.Reported, DiagnosticConfidence.Medium, "APP_CMD_GET_LOG_LEVEL — source-documented uint8", DateTimeOffset.UtcNow);
    }

    public async Task<DiagnosticValue<bool>> GetSysReadyStatusAsync(CancellationToken cancellationToken = default)
    {
        var frame = await QueryAsync(BwmCommandCode.GetSysReadyStatus, cancellationToken: cancellationToken).ConfigureAwait(false);
        return DecodeByteBool(frame, "APP_CMD_GET_SYS_READY_STATUS");
    }

    /// <summary>
    /// Produces a human-readable identity assessment. A BWM endpoint is not
    /// treated as proof of PM5 hardware: PM3/Iceman/RRG variants require
    /// independent USB/hardware evidence before any family or firmware branch
    /// is recommended.
    /// </summary>
    public async Task<BwmIdentityAssessment> AssessIdentityAsync(
        string? usbVidPid = null,
        string? armFirmware = null,
        string? fpgaFirmware = null,
        CancellationToken cancellationToken = default)
    {
        var model = await GetDeviceModelIdAsync(cancellationToken).ConfigureAwait(false);
        var version = await GetVersionInfoAsync(cancellationToken).ConfigureAwait(false);

        return BwmIdentityInspector.Assess(
            model.Value,
            version.Value,
            usbVidPid,
            armFirmware,
            fpgaFirmware);
    }

    public async Task<ProxmarkDeviceInfo> ReadDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        var unknownString = Unknown<string>("Not queried by BwmReadOnlyAdapter");
        var unknownLong = Unknown<long>("Not queried by BwmReadOnlyAdapter");
        var unknownDouble = Unknown<double>("Not queried by BwmReadOnlyAdapter");

        var model = await GetDeviceModelIdAsync(cancellationToken).ConfigureAwait(false);
        var bwmFirmware = await GetVersionInfoAsync(cancellationToken).ConfigureAwait(false);

        return new ProxmarkDeviceInfo(
            Model: unknownString,
            HardwareRevision: unknownString,
            UsbVidPid: unknownString,
            ArmFirmware: unknownString,
            FpgaFirmware: unknownString,
            BwmFirmware: bwmFirmware,
            MemoryBytes: unknownLong,
            Esp32Model: new DiagnosticValue<string>(
                model.Value.HasValue ? $"0x{model.Value.Value:X4}" : null,
                model.SourceState,
                model.Confidence,
                "APP_CMD_GET_DEVICE_MODEL — uint16 model ID; this value alone does not prove PM5/PM3/Iceman/RRG hardware family",
                DateTimeOffset.UtcNow),
            WifiStatus: unknownString,
            BluetoothStatus: unknownString,
            PowerStatus: unknownString,
            BatteryVoltage: unknownDouble);
    }

    private async Task<DiagnosticValue<string>> QueryAndDecodeStringAsync(
        BwmCommandCode command, string upstreamName, CancellationToken cancellationToken)
    {
        var frame = await QueryAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (frame is null)
            return Unknown<string>(upstreamName + " — no response");

        var text = Encoding.UTF8.GetString(frame.Payload).TrimEnd('\0');
        return new DiagnosticValue<string>(
            text,
            DiagnosticSourceState.Reported,
            DiagnosticConfidence.Medium,
            upstreamName + " — source-documented string payload; physical device not yet verified",
            DateTimeOffset.UtcNow);
    }

    private async Task<DiagnosticValue<long>> QueryAndDecodeUInt32Async(
        BwmCommandCode command, string upstreamName, CancellationToken cancellationToken)
    {
        var frame = await QueryAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
        return DecodeUInt32(frame, upstreamName);
    }

    private static DiagnosticValue<long> DecodeUInt32(BwmFrame? frame, string source)
    {
        if (frame is null || frame.Payload.Length != 4)
            return new DiagnosticValue<long>(null, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown, source + " — expected 4-byte uint32", DateTimeOffset.UtcNow);

        uint value = BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload);
        return new DiagnosticValue<long>(value, DiagnosticSourceState.Reported, DiagnosticConfidence.Medium, source + " — source-documented little-endian uint32; physical device not yet verified", DateTimeOffset.UtcNow);
    }

    private static DiagnosticValue<ushort> DecodeUInt16(BwmFrame? frame, string source)
    {
        if (frame is null || frame.Payload.Length != 2)
            return new DiagnosticValue<ushort>(null, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown, source + " — expected 2-byte uint16", DateTimeOffset.UtcNow);

        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(frame.Payload);
        return new DiagnosticValue<ushort>(value, DiagnosticSourceState.Reported, DiagnosticConfidence.Medium, source + " — source-documented little-endian uint16; physical device not yet verified", DateTimeOffset.UtcNow);
    }

    private static DiagnosticValue<bool> DecodeByteBool(BwmFrame? frame, string source)
    {
        if (frame is null || frame.Payload.Length != 1)
            return new DiagnosticValue<bool>(false, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown, source + " — expected 1-byte status", DateTimeOffset.UtcNow);

        return new DiagnosticValue<bool>(frame.Payload[0] != 0, DiagnosticSourceState.Reported, DiagnosticConfidence.Medium, source + " — source-documented uint8 status; physical device not yet verified", DateTimeOffset.UtcNow);
    }

    private static DiagnosticValue<T> Unknown<T>(string reason) =>
        new(default, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown, reason, DateTimeOffset.UtcNow);
}
