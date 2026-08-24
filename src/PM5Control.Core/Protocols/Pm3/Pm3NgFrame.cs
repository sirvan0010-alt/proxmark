using System.Buffers.Binary;

namespace PM5Control.Core.Protocols.Pm3;

public sealed record Pm3NgResponse(ushort Command, sbyte Status, sbyte Reason, byte[] Payload, byte[] RawFrame);
public sealed record Pm3NgExchange(byte[] RequestFrame, Pm3NgResponse Response, IReadOnlyList<Pm3NgResponse> DebugFrames);

/// <summary>Minimal PM3 NG framing for the USB/CDC command channel.</summary>
public static class Pm3NgFrame
{
    public const uint CommandMagic = 0x61334D50;
    public const uint ResponseMagic = 0x62334D50;
    public const ushort CommandPostambleMagic = 0x3361;
    public const ushort ResponsePostambleMagic = 0x3362;
    public const int CommandHeaderSize = 8;
    public const int ResponseHeaderSize = 10;
    public const int PostambleSize = 2;
    public const int MaxPayload = 512;

    public static byte[] EncodeCommand(ushort command, ReadOnlySpan<byte> payload = default)
    {
        if (payload.Length > MaxPayload)
            throw new ArgumentOutOfRangeException(nameof(payload));
        var frame = new byte[CommandHeaderSize + payload.Length + PostambleSize];
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(0, 4), CommandMagic);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4, 2), (ushort)(payload.Length | 0x8000));
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(6, 2), command);
        payload.CopyTo(frame.AsSpan(CommandHeaderSize));
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(frame.Length - 2), CommandPostambleMagic);
        return frame;
    }

    public static bool TryDecodeResponse(ReadOnlySpan<byte> frame, out Pm3NgResponse? response)
    {
        response = null;
        if (frame.Length < ResponseHeaderSize + PostambleSize) return false;
        if (BinaryPrimitives.ReadUInt32LittleEndian(frame[..4]) != ResponseMagic) return false;
        var length = BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(4, 2)) & 0x7FFF;
        if (length > MaxPayload) return false;
        var expected = ResponseHeaderSize + length + PostambleSize;
        if (frame.Length != expected) return false;
        if (BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(expected - 2, 2)) != ResponsePostambleMagic) return false;
        var command = BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(8, 2));
        var status = unchecked((sbyte)frame[6]);
        var reason = unchecked((sbyte)frame[7]);
        var payload = frame.Slice(ResponseHeaderSize, length).ToArray();
        response = new Pm3NgResponse(command, status, reason, payload, frame.ToArray());
        return true;
    }

    public static bool TryGetResponseLength(ReadOnlySpan<byte> header, out int totalLength)
    {
        totalLength = 0;
        if (header.Length < ResponseHeaderSize) return false;
        if (BinaryPrimitives.ReadUInt32LittleEndian(header[..4]) != ResponseMagic) return false;
        var length = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(4, 2)) & 0x7FFF;
        if (length > MaxPayload) return false;
        totalLength = ResponseHeaderSize + length + PostambleSize;
        return true;
    }
}
