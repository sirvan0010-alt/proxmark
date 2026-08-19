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

        var crc = BwmCrc16.Compute(result.AsSpan(2, 4 + payload.Length));
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
        ushort calculatedCrc = BwmCrc16.Compute(frame.Slice(2, 4 + length));
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
