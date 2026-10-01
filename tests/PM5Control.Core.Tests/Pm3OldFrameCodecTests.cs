using PM5Control.Core.Devices;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm3OldFrameCodecTests
{
    [Fact]
    public void EncodeCommandWritesOldFrameHeaderLittleEndianAndPadsToFixedSize()
    {
        var data = new byte[] { 0xAA, 0xBB };
        var frame = Pm3OldFrameCodec.EncodeCommand(
            command: 0x1122334455667788,
            arg0: 0x0102030405060708,
            arg1: 0x1112131415161718,
            arg2: 0x2122232425262728,
            data: data);

        Assert.Equal(Pm3OldFrameCodec.FrameLength, frame.Length);
        Assert.Equal(new byte[] { 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 }, frame[..8]);
        Assert.Equal(new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }, frame[8..16]);
        Assert.Equal(data, frame[Pm3OldFrameCodec.HeaderLength..(Pm3OldFrameCodec.HeaderLength + 2)]);
        Assert.All(frame[(Pm3OldFrameCodec.HeaderLength + 2)..], value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public void EncodeCommandRejectsDataLargerThanOldFramePayload()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Pm3OldFrameCodec.EncodeCommand(1, data: new byte[Pm3OldFrameCodec.DataLength + 1]));
    }

    [Fact]
    public void DecodeResponseReadsFixedOldFrame()
    {
        var bytes = Pm3OldFrameCodec.EncodeCommand(
            Pm3OldFrameCodec.CommandDeviceInfo,
            arg0: 0x201,
            arg1: 1,
            arg2: 921600);

        Assert.True(Pm3OldFrameCodec.TryDecodeResponse(bytes, out var frame));
        Assert.NotNull(frame);
        Assert.Equal(Pm3OldFrameCodec.CommandDeviceInfo, frame!.Command);
        Assert.Equal(0x201UL, frame.Arg0);
        Assert.Equal(1UL, frame.Arg1);
        Assert.Equal(921600UL, frame.Arg2);
        Assert.Equal(Pm3OldFrameCodec.DataLength, frame.Data.Length);
    }

    [Fact]
    public void DecodeResponseRejectsTruncatedOldFrame()
    {
        Assert.False(Pm3OldFrameCodec.TryDecodeResponse(new byte[Pm3OldFrameCodec.FrameLength - 1], out var frame));
        Assert.Null(frame);
    }
}

public sealed class Pm5BootloaderDeviceInfoTests
{
    [Fact]
    public void DeviceInfoDetectsBwmStreamAndRespondingEsp()
    {
        var frame = new Pm3OldFrame(
            Pm3OldFrameCodec.CommandDeviceInfo,
            (uint)(Pm5BootloaderFlags.BootromPresent | Pm5BootloaderFlags.CurrentModeBootrom | Pm5BootloaderFlags.UnderstandsBwmStream),
            1,
            921600,
            Array.Empty<byte>());

        Assert.True(Pm5BootloaderDeviceInfo.TryParse(frame, out var info));
        Assert.NotNull(info);
        Assert.True(info!.BootromPresent);
        Assert.True(info.IsInBootromMode);
        Assert.True(info.UnderstandsBwmStream);
        Assert.True(info.BwmModuleResponded);
        Assert.True(info.CanUseBwmWirelessBootloaderTransport);
        Assert.Equal(921600u, info.BwmBaudRate);
    }

    [Fact]
    public void BwmCapableBootloaderWithoutEspResponseIsNotWirelessReady()
    {
        var frame = new Pm3OldFrame(
            Pm3OldFrameCodec.CommandDeviceInfo,
            (uint)Pm5BootloaderFlags.UnderstandsBwmStream,
            1,
            0,
            Array.Empty<byte>());

        Assert.True(Pm5BootloaderDeviceInfo.TryParse(frame, out var info));
        Assert.NotNull(info);
        Assert.True(info!.UnderstandsBwmStream);
        Assert.False(info.BwmModuleResponded);
        Assert.False(info.CanUseBwmWirelessBootloaderTransport);
    }

    [Fact]
    public void OldBootloaderWithoutBwmFlagIsNotWirelessReady()
    {
        var frame = new Pm3OldFrame(
            Pm3OldFrameCodec.CommandDeviceInfo,
            (uint)Pm5BootloaderFlags.BootromPresent,
            1,
            921600,
            Array.Empty<byte>());

        Assert.True(Pm5BootloaderDeviceInfo.TryParse(frame, out var info));
        Assert.NotNull(info);
        Assert.False(info!.UnderstandsBwmStream);
        Assert.False(info.CanUseBwmWirelessBootloaderTransport);
    }

    [Fact]
    public void ParserRejectsOtherCommandsAndOutOfRangeArguments()
    {
        var other = new Pm3OldFrame(0x1234, 0, 0, 0, Array.Empty<byte>());
        var invalid = new Pm3OldFrame(Pm3OldFrameCodec.CommandDeviceInfo, (ulong)uint.MaxValue + 1, 0, 0, Array.Empty<byte>());

        Assert.False(Pm5BootloaderDeviceInfo.TryParse(other, out _));
        Assert.False(Pm5BootloaderDeviceInfo.TryParse(invalid, out _));
    }
}
