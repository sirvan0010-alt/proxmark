using System.Buffers.Binary;
using System.Text;
using PM5Control.Core.Connections;

namespace PM5Control.Core.Protocols.Pm3;

public sealed record Pm3ReadOnlyIdentity(string Hardware, string ArmFirmware, string FpgaFirmware, string Details);

/// <summary>
/// Raw, undecoded result of a single read-only PM3 NG diagnostic command.
/// Payload is intentionally left as bytes: CMD_STATUS's on-wire layout varies
/// across firmware revisions and has not been hardware-verified for PM5, so
/// this class only reports that the device answered, not what the answer means.
/// Confidence is therefore capped at "raw / unparsed" until verified on real hardware.
/// </summary>
public sealed record Pm3RawDiagnostic(string CommandName, bool Success, sbyte Status, sbyte Reason, int PayloadLength);

public static class Pm3ReadOnlyInspector
{
    /// <summary>Sends CMD_STATUS (0x0108). Read-only. Payload is not decoded - see Pm3RawDiagnostic remarks.</summary>
    public static async Task<Pm3RawDiagnostic> QueryStatusAsync(Pm3SerialTransport transport, CancellationToken cancellationToken = default)
    {
        var response = await transport.SendReadOnlyAsync(Pm3CommandCode.Status, cancellationToken).ConfigureAwait(false);
        return new Pm3RawDiagnostic("CMD_STATUS", response.Status == 0, response.Status, response.Reason, response.Payload.Length);
    }

    /// <summary>Sends CMD_PING (0x0109). Read-only liveness check; device is expected to echo status 0.</summary>
    public static async Task<Pm3RawDiagnostic> PingAsync(Pm3SerialTransport transport, CancellationToken cancellationToken = default)
    {
        var response = await transport.SendReadOnlyAsync(Pm3CommandCode.Ping, cancellationToken).ConfigureAwait(false);
        return new Pm3RawDiagnostic("CMD_PING", response.Status == 0, response.Status, response.Reason, response.Payload.Length);
    }

    public static async Task<Pm3ReadOnlyIdentity> InspectAsync(Pm3SerialTransport transport, CancellationToken cancellationToken = default)
    {
        var version = await transport.SendReadOnlyAsync(Pm3CommandCode.Version, cancellationToken).ConfigureAwait(false);
        if (version.Status != 0) throw new InvalidDataException($"CMD_VERSION failed: status={version.Status}, reason={version.Reason}.");

        var capabilities = await transport.SendReadOnlyAsync(Pm3CommandCode.Capabilities, cancellationToken).ConfigureAwait(false);
        if (capabilities.Status != 0) throw new InvalidDataException($"CMD_CAPABILITIES failed: status={capabilities.Status}, reason={capabilities.Reason}.");

        var text = DecodeVersion(version.Payload, out var arm, out var fpga);
        var isPm5 = capabilities.Payload.Length >= 13 && (capabilities.Payload[12] & 0x10) != 0;
        var hardware = isPm5 ? "Proxmark5 - ARM endpoint verified" : "Proxmark3-family ARM endpoint - PM5 flag not asserted";
        return new Pm3ReadOnlyIdentity(hardware, arm, fpga, text.Length == 0 ? $"Read-only PM3 response received; capabilities={capabilities.Payload.Length} bytes." : text);
    }

    private static string DecodeVersion(byte[] payload, out string arm, out string fpga)
    {
        arm = "UNKNOWN";
        fpga = "UNKNOWN";
        if (payload.Length < 12) return "CMD_VERSION payload too short.";
        var length = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(8, 4));
        if (length > payload.Length - 12) return "CMD_VERSION returned an invalid version-string length.";
        var text = Encoding.UTF8.GetString(payload, 12, (int)length).TrimEnd('\0');
        arm = Extract(text, "[ ARM ]");
        fpga = Extract(text, "[ FPGA ]");
        return text;
    }

    private static string Extract(string text, string marker)
    {
        var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return "UNKNOWN";
        var end = text.IndexOf("\n [", index + marker.Length, StringComparison.Ordinal);
        return (end < 0 ? text[index..] : text[index..end]).Trim();
    }
}
