using System.Buffers.Binary;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm3NgFrameTests
{
    [Fact]
    public void DecodeCapabilities_Version7DecodesRdv4AndCompiledFeatures()
    {
        var payload = new byte[13];
        payload[0] = 7; // CAPABILITIES_VERSION per pm3_cmd.h
        payload[9] = 0x80; // LF
        payload[10] = 0b_0100_0001; // Hitag + ISO14443-A
        payload[11] = 0b_0000_1001; // ISO15693 + iCLASS
        payload[12] = 0b_0000_0010; // is_rdv4 (bit 1)

        var report = Pm3ReadOnlyInspector.DecodeCapabilities(payload);

        Assert.True(report.IsKnownSchema);
        Assert.True(report.IsRdv4);
        Assert.Equal(new[] { "LF", "Hitag", "ISO14443-A", "ISO15693", "iCLASS" }, report.EnabledFeatures);
    }

    [Fact]
    public void DecodeCapabilities_UnknownSchemaDoesNotGuess()
    {
        var report = Pm3ReadOnlyInspector.DecodeCapabilities(new byte[] { 9, 0, 0, 0 });

        Assert.False(report.IsKnownSchema);
        Assert.False(report.IsRdv4);
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
