using System.Buffers.Binary;

namespace PM5Control.Core.Protocols.Pm3;

/// <summary>
/// Fixed-size legacy Proxmark bootloader frame. OLD frames have no NG magic
/// or CRC and remain 544 bytes on the wire (32-byte command/argument header
/// plus 512-byte data area).
/// </summary>
public sealed record Pm3OldFrame(
    ulong Command,
    ulong Arg0,
    ulong Arg1,
    ulong Arg2,
    byte[] Data);

public static class Pm3OldFrameCodec
{
    public const int HeaderLength = 32;
    public const int DataLength = 512;
    public const int FrameLength = HeaderLength + DataLength;
    public const ulong CommandDeviceInfo = 0x0000;

    public static byte[] EncodeCommand(
        ulong command,
        ulong arg0 = 0,
        ulong arg1 = 0,
        ulong arg2 = 0,
        ReadOnlySpan<byte> data = default)
    {
        if (data.Length > DataLength)
            throw new ArgumentOutOfRangeException(nameof(data), $"OLD frame data is limited to {DataLength} bytes.");

        var frame = new byte[FrameLength];
        BinaryPrimitives.WriteUInt64LittleEndian(frame.AsSpan(0, 8), command);
        BinaryPrimitives.WriteUInt64LittleEndian(frame.AsSpan(8, 8), arg0);
        BinaryPrimitives.WriteUInt64LittleEndian(frame.AsSpan(16, 8), arg1);
        BinaryPrimitives.WriteUInt64LittleEndian(frame.AsSpan(24, 8), arg2);
        data.CopyTo(frame.AsSpan(HeaderLength, DataLength));
        return frame;
    }

    public static bool TryDecodeResponse(ReadOnlySpan<byte> bytes, out Pm3OldFrame? frame)
    {
        frame = null;
        if (bytes.Length < FrameLength)
            return false;

        var command = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(0, 8));
        var arg0 = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(8, 8));
        var arg1 = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(16, 8));
        var arg2 = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(24, 8));
        frame = new Pm3OldFrame(command, arg0, arg1, arg2, bytes.Slice(HeaderLength, DataLength).ToArray());
        return true;
    }
}
