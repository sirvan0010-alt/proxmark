// PM5 Control Center — BWM read-only adapter
// PURPOSE: query BWM read-only inspector commands (docs/BWM_PROTOCOL.md
// "Read-only Inspector commands") and return structured DiagnosticValue
// results instead of raw bytes.
// SAFETY: this adapter sends ONLY commands documented as read-only (GET_*,
// not SET_*/START_*/STOP_*/OTA_*/REBOOT/RESTORE_TO_FACTORY_SETTINGS). It
// does not implement any mutating command. Do not add write commands here
// without an explicit, separate write-capable adapter and user consent —
// see project non-goals in AI_CONTEXT.md.
//
// NOTE ON PAYLOAD LAYOUT: the wire *framing* (magic/CRC/length) and the
// *command codes* are verified against the official firmware source (see
// docs/BWM_PROTOCOL.md "Verified provenance"). The per-command *payload
// byte layout* (e.g. is free-heap a little-endian uint32? is version info
// a UTF-8 string or a structured blob?) has NOT been verified against a
// real device or the firmware's response-building code yet. Every decoded
// value here is therefore marked DiagnosticConfidence.Low/Medium, never
// High, until a mock device or real PM5/BWM hardware round-trip confirms
// the exact payload shape. Do not upgrade the confidence level without
// that evidence.
//
// NOTE ON TRANSPORT: this adapter assumes IProxmarkTransport.SendAsync
// already returns one complete, framed response for one request (i.e. any
// stream reassembly via BwmStreamParser happens inside the transport
// implementation, not here). This keeps command knowledge out of the
// transport layer per IProxmarkTransport's documented rule, at the cost of
// not seeing broadcasts here — a caller that needs broadcasts should listen
// on BwmEventDispatcher against the same underlying byte stream separately.

using System.Linq;
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

    /// <summary>
    /// Sends a single read-only BWM request and returns the decoded response
    /// frame, or null if the transport failed, the response was malformed,
    /// or the response did not match the request's command code.
    /// </summary>
    public async Task<BwmFrame?> QueryAsync(
        BwmCommandCode command,
        ReadOnlyMemory<byte> payload = default,
        CancellationToken cancellationToken = default)
    {
        var request = BwmFrameCodec.EncodeRequest((ushort)command, payload.Span);
        byte[] responseBytes;
        try
        {
            responseBytes = await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Transport-level failure (disconnect, timeout, etc). Caller sees
            // this as "no answer", not as a decoded-but-wrong response.
            return null;
        }

        if (!BwmFrameCodec.TryDecode(responseBytes, out var frame) || frame is null)
            return null;

        if (frame.Kind != BwmFrameKind.Response)
            return null;

        if (frame.CommandId != (ushort)command)
            return null;

        return frame;
    }

    /// <summary>
    /// Best-effort UTF-8 decode of a response payload, for commands whose
    /// firmware source names them as returning a "(string)" value.
    /// See the class-level NOTE ON PAYLOAD LAYOUT — this is not yet verified
    /// against real hardware, so results carry DiagnosticConfidence.Medium
    /// at most.
    /// </summary>
    private static DiagnosticValue<string> AsReportedString(BwmFrame? frame, string sourceDescription)
    {
        if (frame is null)
        {
            return new DiagnosticValue<string>(
                null, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown,
                sourceDescription + " (no response)", DateTimeOffset.UtcNow);
        }

        string? text;
        try
        {
            text = System.Text.Encoding.UTF8.GetString(frame.Payload).TrimEnd('\0');
        }
        catch (Exception)
        {
            text = null;
        }

        return new DiagnosticValue<string>(
            text,
            text is null ? DiagnosticSourceState.Unknown : DiagnosticSourceState.Reported,
            text is null ? DiagnosticConfidence.Unknown : DiagnosticConfidence.Medium,
            sourceDescription + " — payload layout not yet verified against real device",
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Best-effort little-endian uint32 decode, for commands whose firmware
    /// source doesn't specify layout beyond "get a numeric value". See the
    /// class-level NOTE ON PAYLOAD LAYOUT.
    /// </summary>
    private static DiagnosticValue<long> AsReportedUInt32(BwmFrame? frame, string sourceDescription)
    {
        if (frame is null || frame.Payload.Length < 4)
        {
            return new DiagnosticValue<long>(
                null, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown,
                sourceDescription + " (no response or payload too short)", DateTimeOffset.UtcNow);
        }

        long value = frame.Payload[0]
            | ((long)frame.Payload[1] << 8)
            | ((long)frame.Payload[2] << 16)
            | ((long)frame.Payload[3] << 24);

        return new DiagnosticValue<long>(
            value, DiagnosticSourceState.Reported, DiagnosticConfidence.Medium,
            sourceDescription + " — assumed little-endian uint32, not yet verified against real device",
            DateTimeOffset.UtcNow);
    }

    public Task<DiagnosticValue<string>> GetVersionInfoAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeStringAsync(BwmCommandCode.GetVersionInfo, "APP_CMD_GET_VERSION_INFO", cancellationToken);

    public Task<DiagnosticValue<string>> GetDeviceModelAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeStringAsync(BwmCommandCode.GetDeviceModel, "APP_CMD_GET_DEVICE_MODEL", cancellationToken);

    public Task<DiagnosticValue<string>> GetAppCompileDatetimeAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeStringAsync(BwmCommandCode.GetAppCompileDatetime, "APP_CMD_GET_APP_COMPILE_DATETIME", cancellationToken);

    public Task<DiagnosticValue<long>> GetSysFreeHeapAsync(CancellationToken cancellationToken = default) =>
        QueryAndDecodeUInt32Async(BwmCommandCode.GetSysFreeHeap, "APP_CMD_GET_SYS_FREE_HEAP", cancellationToken);

    /// <summary>
    /// Base MAC address, returned as a colon-separated hex string
    /// (e.g. "AA:BB:CC:DD:EE:FF") if the payload is exactly 6 bytes.
    /// </summary>
    public async Task<DiagnosticValue<string>> GetSysBaseMacAddrAsync(CancellationToken cancellationToken = default)
    {
        var frame = await QueryAsync(BwmCommandCode.GetSysBaseMacAddr, cancellationToken: cancellationToken).ConfigureAwait(false);
        const string source = "APP_CMD_GET_SYS_BASE_MAC_ADDR";

        if (frame is null || frame.Payload.Length != 6)
        {
            return new DiagnosticValue<string>(
                null, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown,
                source + " (no response or unexpected payload length)", DateTimeOffset.UtcNow);
        }

        var mac = string.Join(":", frame.Payload.Select(b => b.ToString("X2")));
        return new DiagnosticValue<string>(
            mac, DiagnosticSourceState.Reported, DiagnosticConfidence.Medium,
            source + " — assumed raw 6-byte MAC, not yet verified against real device",
            DateTimeOffset.UtcNow);
    }

    private async Task<DiagnosticValue<string>> QueryAndDecodeStringAsync(
        BwmCommandCode command, string upstreamName, CancellationToken cancellationToken)
    {
        var frame = await QueryAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
        return AsReportedString(frame, upstreamName);
    }

    private async Task<DiagnosticValue<long>> QueryAndDecodeUInt32Async(
        BwmCommandCode command, string upstreamName, CancellationToken cancellationToken)
    {
        var frame = await QueryAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
        return AsReportedUInt32(frame, upstreamName);
    }

    /// <summary>
    /// Populates the ESP32/BWM-scoped fields of ProxmarkDeviceInfo
    /// (Esp32Model, BwmFirmware) from read-only BWM queries. Fields that
    /// belong to the main PM5 ARM/FPGA subsystem (Model, HardwareRevision,
    /// UsbVidPid, ArmFirmware, FpgaFirmware) are NOT BWM commands at all —
    /// they are left Unknown here rather than guessed, and must come from a
    /// separate main-firmware protocol adapter that does not exist yet.
    /// WifiStatus/BluetoothStatus/PowerStatus/BatteryVoltage/MemoryBytes are
    /// also left Unknown pending mapping to specific verified commands and
    /// hardware confirmation that the fuel-gauge/charger telemetry is
    /// actually exposed over this protocol (see docs/BWM_PROTOCOL.md
    /// "Battery/power").
    /// </summary>
    public async Task<ProxmarkDeviceInfo> ReadDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        var unknownString = Unknown<string>("Not queried by BwmReadOnlyAdapter");
        var unknownLong = Unknown<long>("Not queried by BwmReadOnlyAdapter");
        var unknownDouble = Unknown<double>("Not queried by BwmReadOnlyAdapter");

        var esp32Model = await GetDeviceModelAsync(cancellationToken).ConfigureAwait(false);
        var bwmFirmware = await GetVersionInfoAsync(cancellationToken).ConfigureAwait(false);

        return new ProxmarkDeviceInfo(
            Model: unknownString,
            HardwareRevision: unknownString,
            UsbVidPid: unknownString,
            ArmFirmware: unknownString,
            FpgaFirmware: unknownString,
            BwmFirmware: bwmFirmware,
            MemoryBytes: unknownLong,
            Esp32Model: esp32Model,
            WifiStatus: unknownString,
            BluetoothStatus: unknownString,
            PowerStatus: unknownString,
            BatteryVoltage: unknownDouble);
    }

    private static DiagnosticValue<T> Unknown<T>(string reason) =>
        new(default, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown, reason, DateTimeOffset.UtcNow);
}
