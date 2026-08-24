using System.Buffers.Binary;
using System.Text;
using PM5Control.Core.Connections;

namespace PM5Control.Core.Protocols.Pm3;

public sealed record Pm3ReadOnlyIdentity(string Hardware, string ArmFirmware, string FpgaFirmware, string Details);
public sealed record Pm3CapabilitiesReport(int SchemaVersion, bool IsKnownSchema, bool? IsPm5, IReadOnlyList<string> EnabledFeatures, byte[] RawPayload);
public sealed record Pm3RawDiagnostic(
    string CommandName,
    ushort ExpectedCommand,
    ushort ResponseCommand,
    bool ResponseCommandMatches,
    bool Success,
    sbyte Status,
    sbyte Reason,
    byte[] RequestFrame,
    byte[] Payload,
    byte[] RawResponseFrame,
    IReadOnlyList<Pm3NgResponse> DebugFrames)
{
    public int PayloadLength => Payload.Length;
}

public static class Pm3ReadOnlyInspector
{
    public static async Task<Pm3RawDiagnostic> QueryStatusAsync(Pm3SerialTransport transport, CancellationToken cancellationToken = default)
        => await QueryAsync(transport, Pm3CommandCode.Status, "CMD_STATUS", cancellationToken).ConfigureAwait(false);

    public static async Task<Pm3RawDiagnostic> PingAsync(Pm3SerialTransport transport, CancellationToken cancellationToken = default)
        => await QueryAsync(transport, Pm3CommandCode.Ping, "CMD_PING", cancellationToken).ConfigureAwait(false);

    public static async Task<Pm3RawDiagnostic> QueryAsync(Pm3SerialTransport transport, ushort command, string name, CancellationToken cancellationToken = default)
    {
        if (!Pm3CommandCode.IsSafeReadOnlyProbe(command))
            throw new ArgumentOutOfRangeException(nameof(command), $"Command 0x{command:X4} is not on the safe read-only probe whitelist.");

        var exchange = await transport.SendReadOnlyAsync(command, cancellationToken).ConfigureAwait(false);
        var response = exchange.Response;
        return new Pm3RawDiagnostic(
            name,
            command,
            response.Command,
            response.Command == command,
            response.Status == 0,
            response.Status,
            response.Reason,
            exchange.RequestFrame,
            response.Payload,
            response.RawFrame,
            exchange.DebugFrames);
    }

    public static async Task<Pm3ReadOnlyIdentity> QueryVersionAsync(Pm3SerialTransport transport, CancellationToken cancellationToken = default)
    {
        var version = (await transport.SendReadOnlyAsync(Pm3CommandCode.Version, cancellationToken).ConfigureAwait(false)).Response;
        if (version.Status != 0) throw new InvalidDataException($"CMD_VERSION failed: status={version.Status}, reason={version.Reason}.");
        var text = DecodeVersion(version.Payload, out var arm, out var fpga);
        return new Pm3ReadOnlyIdentity("PM3-family ARM endpoint verified; hardware family not yet confirmed", arm, fpga, text);
    }

    public static async Task<Pm3CapabilitiesReport> QueryCapabilitiesAsync(Pm3SerialTransport transport, CancellationToken cancellationToken = default)
    {
        var response = (await transport.SendReadOnlyAsync(Pm3CommandCode.Capabilities, cancellationToken).ConfigureAwait(false)).Response;
        if (response.Status != 0) throw new InvalidDataException($"CMD_CAPABILITIES failed: status={response.Status}, reason={response.Reason}.");
        return DecodeCapabilities(response.Payload);
    }

    public static async Task<(Pm3ReadOnlyIdentity Identity, Pm3CapabilitiesReport Capabilities)> InspectAsync(Pm3SerialTransport transport, CancellationToken cancellationToken = default)
    {
        var identity = await QueryVersionAsync(transport, cancellationToken).ConfigureAwait(false);
        var capabilities = await QueryCapabilitiesAsync(transport, cancellationToken).ConfigureAwait(false);
        var hardware = capabilities.IsPm5 switch
        {
            true => "Proxmark5 - firmware reported",
            false => "Proxmark3-family - PM5 flag not asserted",
            null => "UNKNOWN - CMD_CAPABILITIES schema is not recognised"
        };
        return (identity with { Hardware = hardware }, capabilities);
    }

    public static Pm3CapabilitiesReport DecodeCapabilities(byte[] payload)
    {
        var version = payload.Length == 0 ? -1 : payload[0];
        if (version != 8 || payload.Length < 13)
            return new Pm3CapabilitiesReport(version, false, null, Array.Empty<string>(), payload);

        var features = new List<string>();
        AddIf(features, payload[9], 7, "LF");
        AddIf(features, payload[10], 0, "Hitag");
        AddIf(features, payload[10], 1, "EM4x50");
        AddIf(features, payload[10], 2, "EM4x70");
        AddIf(features, payload[10], 3, "ZX8211");
        AddIf(features, payload[10], 4, "HF sniff");
        AddIf(features, payload[10], 5, "HF plot");
        AddIf(features, payload[10], 6, "ISO14443-A");
        AddIf(features, payload[10], 7, "ISO14443-B");
        AddIf(features, payload[11], 0, "ISO15693");
        AddIf(features, payload[11], 1, "FeliCa");
        AddIf(features, payload[11], 2, "LEGIC");
        AddIf(features, payload[11], 3, "iCLASS");
        AddIf(features, payload[11], 4, "SEOS");
        AddIf(features, payload[11], 5, "NFC barcode");
        return new Pm3CapabilitiesReport(version, true, (payload[12] & 0x10) != 0, features, payload);
    }

    private static void AddIf(List<string> features, byte value, int bit, string name)
    {
        if ((value & (1 << bit)) != 0) features.Add(name);
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
