using System.Buffers.Binary;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm3NgFrameTests
{
    [Fact]
    public void DecodeCapabilities_Version11DecodesPm5AndAppendedFields()
    {
        var payload = new byte[18];
        payload[0] = 11; // current CAPABILITIES_VERSION per upstream pm3_cmd.h
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(1, 4), 460800);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(5, 4), 65536);
        payload[9] = 0x80; // LF
        payload[10] = 0b_0100_0001; // Hitag + ISO14443-A
        payload[11] = 0b_0000_1001; // ISO15693 + iCLASS
        payload[12] = 0b_0001_0010; // is_rdv4 (bit 1) + is_pm5 (bit 4)
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(13, 2), 4064); // max_cmd_data_size, v9+
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(15, 2), 8192); // em_size, v11+
        payload[17] = 0x01; // em_allocated

        var report = Pm3ReadOnlyInspector.DecodeCapabilities(payload);

        Assert.True(report.IsKnownSchema);
        Assert.Equal(11, report.SchemaVersion);
        Assert.True(report.IsRdv4);
        Assert.True(report.IsPm5);
        Assert.False(report.IsPm5StandardAntenna);
        Assert.Equal(460800u, report.BaudRate);
        Assert.Equal(65536u, report.BigBufferSize);
        Assert.Equal((ushort)4064, report.MaxCommandDataSize);
        Assert.Equal((ushort)8192, report.EmulatorSize);
        Assert.True(report.EmulatorAllocated);
        Assert.Equal(
            new[] { "LF support", "Hitag", "ISO14443-A", "ISO15693", "iCLASS", "RDV4 hardware", "PM5 hardware" },
            report.EnabledFeatures);
    }

    [Fact]
    public void DecodeCapabilities_UnknownSchemaDoesNotGuess()
    {
        var report = Pm3ReadOnlyInspector.DecodeCapabilities(new byte[] { 12, 0, 0, 0 });

        Assert.False(report.IsKnownSchema);
        Assert.False(report.IsRdv4);
        Assert.False(report.IsPm5);
        Assert.Empty(report.EnabledFeatures);
    }

    [Fact]
    public void DecodeCapabilities_RejectsKnownVersionWithTruncatedPayload()
    {
        var report = Pm3ReadOnlyInspector.DecodeCapabilities(new byte[] { 11, 0, 0, 0 });

        Assert.False(report.IsKnownSchema);
        Assert.Equal(11, report.SchemaVersion);
        Assert.Empty(report.EnabledFeatures);
    }

    [Fact]
    public void EncodeCommand_UsesPm3aMagic()
    {
        var frame = Pm3NgFrame.EncodeCommand(Pm3CommandCode.Version);
        Assert.Equal(10, frame.Length);
        Assert.Equal(Pm3NgFrame.CommandMagic, BinaryPrimitives.ReadUInt32LittleEndian(frame.AsSpan(0, 4)));
        Assert.Equal(0x8000, BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(4, 2)));
        Assert.Equal(Pm3CommandCode.Version, BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(6, 2)));
        Assert.Equal(Pm3NgFrame.CommandPostambleMagic, BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(8, 2)));
    }

    [Fact]
    public void EncodeCommand_AcceptsFullPm5Payload()
    {
        var payload = Enumerable.Repeat((byte)0xA5, Pm3NgFrame.MaxPayload).ToArray();

        var frame = Pm3NgFrame.EncodeCommand(0x1234, payload);

        Assert.Equal(Pm3NgFrame.CommandHeaderSize + Pm3NgFrame.MaxPayload + Pm3NgFrame.PostambleSize, frame.Length);
        Assert.Equal((ushort)(0x8000 | Pm3NgFrame.MaxPayload),
            BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(4, 2)));
    }

    [Fact]
    public void EncodeCommand_RejectsPayloadAbovePm5Limit()
    {
        var payload = Enumerable.Repeat((byte)0xA5, Pm3NgFrame.MaxPayload + 1).ToArray();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Pm3NgFrame.EncodeCommand(0x1234, payload));
    }

    [Fact]
    public void DecodeResponse_AcceptsUsbPostamble()
    {
        var payload = new byte[] { 1, 2, 3 };
        var frame = new byte[Pm3NgFrame.ResponseHeaderSize + payload.Length + 2];
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(0, 4), Pm3NgFrame.ResponseMagic);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4, 2), (ushort)(0x8000 | payload.Length));
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(8, 2), Pm3CommandCode.Version);
        payload.CopyTo(frame.AsSpan(10));
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(frame.Length - 2), Pm3NgFrame.ResponsePostambleMagic);
        Assert.True(Pm3NgFrame.TryDecodeResponse(frame, out var response));
        Assert.NotNull(response);
        Assert.Equal(Pm3CommandCode.Version, response!.Command);
        Assert.Equal(payload, response.Payload);
    }
}
