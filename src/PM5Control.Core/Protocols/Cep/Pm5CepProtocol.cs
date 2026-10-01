namespace PM5Control.Core.Protocols.Cep;

/// <summary>
/// Source-backed PM5 Type-C Extended Port (CEP) transport constants.
/// CEP is a PM5↔Flipper Zero transport; it is not a direct PC USB/BLE
/// transport. The host-side Control Center must treat Flipper as the
/// external transport endpoint until a separate host bridge is verified.
/// </summary>
public static class Pm5CepProtocol
{
    public const string UpstreamRepository = "RfidResearchGroup/proxmark3";
    public const string UpstreamCommit = "2b5e3e51ebc09963fcb070f012527e0405befe4f";
    public const string CurrentDefaultOnCommit = "16024ed4a183864d7d9e4146e53f47d491aa4cc4";

    public const int HandshakeBaudRate = 2400;
    public const string HandshakeString = "iamf0rupm5";
    public const byte HandshakeStx = 0x02;
    public const string HandshakeReply = "yes";

    public const byte CcControllerAddress = 0x47;
    public const byte CcStatusRegister = 0x09;

    /// <summary>
    /// CEP carries standard PM3 NG frames after attachment. The CEP SPI
    /// transport prefixes each frame with a little-endian uint16 length.
    /// </summary>
    public const int LengthPrefixBytes = 2;

    public const string HardwareVerificationBoundary =
        "Handshake and CEP attach are source/hardware evidenced upstream; " +
        "post-handshake NG frame transport is not yet hardware-verified " +
        "by this Control Center.";

    public static bool IsHandshakeResponse(ReadOnlySpan<byte> bytes)
        => bytes.SequenceEqual("yes"u8);

    public static byte[] BuildHandshakeProbe()
    {
        var result = new byte[1 + HandshakeString.Length];
        result[0] = HandshakeStx;
        "iamf0rupm5"u8.CopyTo(result.AsSpan(1));
        return result;
    }

    public static bool TryReadLength(ReadOnlySpan<byte> prefix, out ushort length)
    {
        if (prefix.Length < LengthPrefixBytes)
        {
            length = 0;
            return false;
        }

        length = (ushort)(prefix[0] | (prefix[1] << 8));
        return true;
    }
}
