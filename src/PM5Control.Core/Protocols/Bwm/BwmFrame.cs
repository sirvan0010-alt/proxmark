// PM5 Control Center
// PURPOSE: represent and encode/decode one BWM binary frame.
// IMPORTANT: this is protocol infrastructure, not a CLI wrapper.
namespace PM5Control.Core.Protocols.Bwm;

public enum BwmFrameKind
{
    Request,
    Response,
    Broadcast
}

public sealed record BwmFrame(BwmFrameKind Kind, ushort CommandId, byte[] Payload);

public static class BwmFrameCodec
{
    public static byte[] EncodeRequest(ushort commandId, ReadOnlySpan<byte> payload)
        => Encode(BwmProtocolConstants.RequestMagic, commandId, payload);

    public static byte[] EncodeResponse(ushort commandId, ReadOnlySpan<byte> payload)
        => Encode(BwmProtocolConstants.ResponseMagic, commandId, payload);

    public static byte[] EncodeBroadcast(ushort commandId, ReadOnlySpan<byte> payload)
        => Encode(BwmProtocolConstants.BroadcastMagic, commandId, payload);

    private static byte[] Encode(ushort magic, ushort commandId, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(payload));

        var result = new byte[BwmProtocolConstants.HeaderSize + payload.Length + BwmProtocolConstants.CrcSize];
        WriteU16(result, 0, magic);
        WriteU16(result, 2, commandId);
        WriteU16(result, 4, (ushort)payload.Length);
        payload.CopyTo(result.AsSpan(BwmProtocolConstants.HeaderSize));

        // CRC covers magic + commandId + length + payload (everything up to
        // the CRC field itself). VERIFIED against the official firmware
        // source: RfidResearchGroup/Proxmark5_BWM_esp32, commit
        // b918166128e05455c2dcb4e232216d453bbf29ee (2026-08-08),
        // components/app_uart_cmd/app_cmd_uart.c, uart_build_and_send():
        // crc16_ccitt(pkt_buf, idx, CRC16_INIT) where idx already includes
        // the 2 header/magic bytes. This is not a hypothesis pulled from a
        // different (PM3 NG) protocol — it is read directly from the BWM
        // firmware source this client targets. See docs/BWM_PROTOCOL.md
        // "Verified provenance". Do NOT change this scope without new
        // evidence from the same or a newer verified firmware source.
        var crc = BwmCrc16.Compute(result.AsSpan(0, BwmProtocolConstants.HeaderSize + payload.Length));
        WriteU16(result, result.Length - 2, crc);
        return result;
    }

    public static bool TryDecode(ReadOnlySpan<byte> frame, out BwmFrame? result)
    {
        result = null;
        if (frame.Length < BwmProtocolConstants.HeaderSize + BwmProtocolConstants.CrcSize)
            return false;

        ushort magic = ReadU16(frame, 0);
        BwmFrameKind kind = magic switch
        {
            BwmProtocolConstants.RequestMagic => BwmFrameKind.Request,
            BwmProtocolConstants.ResponseMagic => BwmFrameKind.Response,
            BwmProtocolConstants.BroadcastMagic => BwmFrameKind.Broadcast,
            _ => default
        };
        if (magic is not (BwmProtocolConstants.RequestMagic or BwmProtocolConstants.ResponseMagic or BwmProtocolConstants.BroadcastMagic))
            return false;

        ushort commandId = ReadU16(frame, 2);
        ushort length = ReadU16(frame, 4);
        int expected = BwmProtocolConstants.HeaderSize + length + BwmProtocolConstants.CrcSize;
        if (frame.Length != expected)
            return false;

        ushort receivedCrc = ReadU16(frame, expected - 2);
        // See Encode(): CRC covers magic + commandId + length + payload.
        ushort calculatedCrc = BwmCrc16.Compute(frame.Slice(0, BwmProtocolConstants.HeaderSize + length));
        if (receivedCrc != calculatedCrc)
            return false;

        result = new BwmFrame(kind, commandId, frame.Slice(BwmProtocolConstants.HeaderSize, length).ToArray());
        return true;
    }

    private static ushort ReadU16(ReadOnlySpan<byte> data, int offset)
        => (ushort)(data[offset] | (data[offset + 1] << 8));

    private static void WriteU16(Span<byte> data, int offset, ushort value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
    }
}
