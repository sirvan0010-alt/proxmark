// PM5 Control Center — BWM read-only adapter
// PURPOSE: query BWM read-only inspector commands (docs/BWM_PROTOCOL.md
// "Read-only Inspector commands") and return structured DiagnosticValue
// results instead of raw bytes.
// SAFETY: this adapter sends ONLY the explicitly allow-listed read-only
// commands below. It does not rely on enum naming or comments to enforce
// that property. Mutating commands are rejected before a transport call.
// Do not add write commands here; use a separate explicitly write-capable
// adapter with its own safety/consent contract — see AI_CONTEXT.md.
//
// NOTE ON PAYLOAD LAYOUT: the wire framing (magic/CRC/length) and the
// command codes are verified against the official firmware source (see
// docs/BWM_PROTOCOL.md "Verified provenance"). The per-command payload
// byte layout has NOT been verified against a real device or the firmware's
// response-building code yet. Every decoded value here is therefore marked
// DiagnosticConfidence.Low/Medium, never High, until evidence confirms the
// exact payload shape.
//
// NOTE ON TRANSPORT: this adapter assumes IProxmarkTransport.SendAsync
// returns one complete, framed response for one request. Stream reassembly
// via BwmStreamParser belongs in the transport/protocol integration layer.

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
    /// Sends one explicitly allow-listed read-only BWM request and returns
    /// the decoded response frame, or null if transport/protocol validation
    /// fails. Commands outside the allow-list are rejected before SendAsync.
    /// </summary>
    public async Task<BwmFrame?> QueryAsync(
        BwmCommandCode command,
        ReadOnlyMemory<byte> payload = default,
        CancellationToken cancellationToken = default)
    {
        if (!IsReadOnlyCommand(command))
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
            // Transport-level failure (disconnect, timeout, etc.). Caller
            // sees this as no usable answer; cancellation remains observable.
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

    private static bool IsReadOnlyCommand(BwmCommandCode command) => command switch
    {
        BwmCommandCode.GetVersionInfo => true,
        BwmCommandCode.GetDeviceModel => true,
        BwmCommandCode.GetAppCompileDatetime => true,
        BwmCommandCode.GetSysFreeHeap => true,
        BwmCommandCode.GetSysBaseMacAddr => true,
        _ => false
    };

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
