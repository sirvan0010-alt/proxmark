using System.Buffers.Binary;
using System.Text;
using PM5Control.Core.Connections;

namespace PM5Control.Core.Protocols.Pm3;

public sealed record Pm3ReadOnlyIdentity(string Hardware, string ArmFirmware, string FpgaFirmware, string Details);

public static class Pm3ReadOnlyInspector
{
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
