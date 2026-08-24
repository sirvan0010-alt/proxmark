using System.Buffers.Binary;
using System.Text;
using PM5Control.Core.Connections;

namespace PM5Control.Core.Protocols.Pm3;

public sealed record Pm3ReadOnlyIdentity(string Hardware, string ArmFirmware, string FpgaFirmware, string Details);

/// <summary>
/// Decoded CMD_CAPABILITIES response. The v6 wire layout is the current upstream
/// PM3/RRG capabilities_t layout: 1 byte version, uint32 baudrate, uint32 bigbuf
/// size and four bytes of bool bit-fields (25 defined flags + padding).
/// </summary>
public sealed record Pm3CapabilitiesReport(
    int SchemaVersion,
    bool IsKnownSchema,
    bool IsRdv4,
    bool ViaUsb,
    bool ViaFpc,
    uint BaudRate,
    uint BigBufferSize,
    IReadOnlyList<string> EnabledFeatures,
    byte[] RawPayload);

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
    IReadOnlyList<Pm3NgResponse> DebugFrames,
    IReadOnlyList<Pm3NgResponse> UnmatchedResponses)
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
            exchange.DebugFrames,
            exchange.UnmatchedResponses);
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
        var hardware = capabilities.IsKnownSchema
            ? $"PM3-family ARM endpoint verified; capabilities v{capabilities.SchemaVersion}; USB={(capabilities.ViaUsb ? "yes" : "no")}; RDV4={(capabilities.IsRdv4 ? "yes" : "no")}."
            : "PM3-family ARM endpoint verified - CMD_CAPABILITIES schema version not recognised by this build.";
        return (identity with { Hardware = hardware }, capabilities);
    }

    /// <summary>
    /// Decodes the upstream CAPABILITIES_VERSION 6 layout from pm3_cmd.h.
    /// Wire layout for the real PM5 response is 13 bytes:
    /// [version][baudrate:u32 LE][bigbuf_size:u32 LE][flags0..flags3].
    /// The bool bit-fields are allocated in four one-byte units by the ARM toolchain.
    /// Defined flags occupy the first 25 bits; the remaining bits are padding.
    /// </summary>
    public static Pm3CapabilitiesReport DecodeCapabilities(byte[] payload)
    {
        var version = payload.Length == 0 ? -1 : payload[0];
        if (version != 6 || payload.Length < 13)
            return new Pm3CapabilitiesReport(version, false, false, false, false, 0, 0, Array.Empty<string>(), payload);

        var baudRate = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(1, 4));
        var bigBufferSize = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(5, 4));
        var features = new List<string>();

        // flags0: via_fpc, via_usb, compiled_with_flash, compiled_with_smartcard,
        // compiled_with_fpc_usart, compiled_with_fpc_usart_dev, compiled_with_fpc_usart_host, compiled_with_lf
        AddIf(features, payload[9], 0, "FPC transport");
        var viaUsb = Has(payload[9], 1);
        AddIf(features, payload[9], 2, "Flash support");
        AddIf(features, payload[9], 3, "Smartcard support");
        AddIf(features, payload[9], 4, "FPC USART");
        AddIf(features, payload[9], 5, "FPC USART device");
        AddIf(features, payload[9], 6, "FPC USART host");
        AddIf(features, payload[9], 7, "LF support");

        // flags1: Hitag, EM4x50, EM4x70, ZX8211, HF sniff, HF plot, ISO14443-A/B
        AddIf(features, payload[10], 0, "Hitag");
        AddIf(features, payload[10], 1, "EM4x50");
        AddIf(features, payload[10], 2, "EM4x70");
        AddIf(features, payload[10], 3, "ZX8211");
        AddIf(features, payload[10], 4, "HF sniff");
        AddIf(features, payload[10], 5, "HF plot");
        AddIf(features, payload[10], 6, "ISO14443-A");
        AddIf(features, payload[10], 7, "ISO14443-B");

        // flags2: ISO15693, FeliCa, LEGIC, iCLASS, NFC barcode, LCD,
        // hardware flash available, hardware smartcard available
        AddIf(features, payload[11], 0, "ISO15693");
        AddIf(features, payload[11], 1, "FeliCa");
        AddIf(features, payload[11], 2, "LEGIC");
        AddIf(features, payload[11], 3, "iCLASS");
        AddIf(features, payload[11], 4, "NFC barcode");
        AddIf(features, payload[11], 5, "LCD");
        AddIf(features, payload[11], 6, "Hardware flash");
        AddIf(features, payload[11], 7, "Hardware smartcard");

        // flags3: is_rdv4 is the first bit; remaining bits are currently undefined/padding.
        var isRdv4 = Has(payload[12], 0);

        return new Pm3CapabilitiesReport(
            version,
            true,
            isRdv4,
            viaUsb,
            Has(payload[9], 0),
            baudRate,
            bigBufferSize,
            features,
            payload);
    }

    private static bool Has(byte value, int bit) => (value & (1 << bit)) != 0;

    private static void AddIf(List<string> features, byte value, int bit, string name)
    {
        if (Has(value, bit)) features.Add(name);
    }

    private static string DecodeVersion(byte[] payload, out string arm, out string fpga)
    {
        arm = "UNKNOWN";
        fpga = "UNKNOWN";
        if (payload.Length < 12) return "CMD_VERSION payload too short.";
        var length = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(8, 4));
        if (length == 0 || length > payload.Length - 12)
            return "CMD_VERSION returned an invalid version-string length.";

        var text = Encoding.UTF8.GetString(payload, 12, (int)length).TrimEnd('\0');
        arm = ExtractSection(text, "ARM");
        fpga = ExtractSection(text, "FPGA");
        return text;
    }

    private static string ExtractSection(string text, string section)
    {
        var marker = $"[ {section} ]";
        var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return "UNKNOWN";
        var start = index + marker.Length;
        while (start < text.Length && (text[start] == ' ' || text[start] == '\r' || text[start] == '\n')) start++;
        var next = text.IndexOf("\n [", start, StringComparison.Ordinal);
        return (next < 0 ? text[start..] : text[start..next]).Trim();
    }
}
