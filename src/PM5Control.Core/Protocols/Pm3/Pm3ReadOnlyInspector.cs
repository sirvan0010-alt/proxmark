using System.Buffers.Binary;
using System.Text;
using PM5Control.Core.Connections;

namespace PM5Control.Core.Protocols.Pm3;

public sealed record Pm3ReadOnlyIdentity(string Hardware, string ArmFirmware, string FpgaFirmware, string Details);

/// <summary>
/// Decoded CMD_CAPABILITIES response, matching capabilities_t in pm3_cmd.h (CAPABILITIES_VERSION 7).
/// IMPORTANT: this struct has NO field that identifies "this is a PM5" - only RDV4/flash/smartcard
/// hardware flags and per-protocol compiled-in flags. Do not infer PM5 vs PM3 from this alone.
/// </summary>
public sealed record Pm3CapabilitiesReport(int SchemaVersion, bool IsKnownSchema, bool IsRdv4, IReadOnlyList<string> EnabledFeatures, byte[] RawPayload);
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
            ? $"PM3-family ARM endpoint verified (RDV4 hardware flag: {(capabilities.IsRdv4 ? "yes" : "no")}). " +
              "capabilities_t has no PM5-specific bit, so PM5 vs PM3 cannot be confirmed from this response alone."
            : "PM3-family ARM endpoint verified - CMD_CAPABILITIES schema version not recognised by this build.";
        return (identity with { Hardware = hardware }, capabilities);
    }

    /// <summary>
    /// Decodes CMD_CAPABILITIES against the real capabilities_t layout from pm3_cmd.h:
    /// uint8 version; uint32 baudrate; uint32 bigbuf_size; then 26 packed bit flags (bytes 9-12).
    /// Bit order below matches the field declaration order in the struct, assuming the
    /// firmware was built with GCC's standard LSB-first bitfield packing (the toolchain
    /// used for both ARM and the desktop client). This has not been cross-checked against
    /// a hex dump from real hardware, so treat the exact bit positions as Medium confidence
    /// until verified against your PM5's actual CMD_CAPABILITIES bytes.
    /// </summary>
    public static Pm3CapabilitiesReport DecodeCapabilities(byte[] payload)
    {
        var version = payload.Length == 0 ? -1 : payload[0];
        if (version != 7 || payload.Length < 13)
            return new Pm3CapabilitiesReport(version, false, false, Array.Empty<string>(), payload);

        var features = new List<string>();
        // byte 9: via_fpc, via_usb, flash, smartcard, fpc_usart, fpc_usart_dev, fpc_usart_host, lf
        AddIf(features, payload[9], 2, "RDV4 flash");
        AddIf(features, payload[9], 3, "RDV4 smartcard");
        AddIf(features, payload[9], 4, "FPC USART");
        AddIf(features, payload[9], 7, "LF");
        // byte 10: hitag, em4x50, em4x70, zx8211, hfsniff, hfplot, iso14443a, iso14443b
        AddIf(features, payload[10], 0, "Hitag");
        AddIf(features, payload[10], 1, "EM4x50");
        AddIf(features, payload[10], 2, "EM4x70");
        AddIf(features, payload[10], 3, "ZX8211");
        AddIf(features, payload[10], 4, "HF sniff");
        AddIf(features, payload[10], 5, "HF plot");
        AddIf(features, payload[10], 6, "ISO14443-A");
        AddIf(features, payload[10], 7, "ISO14443-B");
        // byte 11: iso15693, felica, legicrf, iclass, seos, nfcbarcode, lcd, hw_available_flash
        AddIf(features, payload[11], 0, "ISO15693");
        AddIf(features, payload[11], 1, "FeliCa");
        AddIf(features, payload[11], 2, "LEGIC");
        AddIf(features, payload[11], 3, "iCLASS");
        AddIf(features, payload[11], 4, "SEOS");
        AddIf(features, payload[11], 5, "NFC barcode");
        AddIf(features, payload[11], 6, "LCD");
        // byte 12: hw_available_smartcard(bit0), is_rdv4(bit1)
        var isRdv4 = (payload[12] & 0x02) != 0;
        return new Pm3CapabilitiesReport(version, true, isRdv4, features, payload);
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
