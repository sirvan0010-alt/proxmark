using System.Buffers.Binary;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm3NgResponseStreamParserTests
{
    [Fact]
    public void ParsesSingleResponse()
    {
        var frame = MakeResponse(Pm3CommandCode.Version, new byte[] { 1, 2, 3 });
        var parser = new Pm3NgResponseStreamParser();
        Pm3NgResponse? received = null;
        parser.ResponseReceived += response => received = response;

        parser.Append(frame);

        Assert.NotNull(received);
        Assert.Equal(Pm3CommandCode.Version, received!.Command);
        Assert.Equal(new byte[] { 1, 2, 3 }, received.Payload);
    }

    [Fact]
    public void ParsesResponseSplitAcrossAppends()
    {
        var frame = MakeResponse(Pm3CommandCode.Status, new byte[] { 9, 8, 7, 6 });
        var parser = new Pm3NgResponseStreamParser();
        var count = 0;
        parser.ResponseReceived += _ => count++;

        parser.Append(frame.AsSpan(0, 5));
        Assert.Equal(0, count);
        parser.Append(frame.AsSpan(5));

        Assert.Equal(1, count);
    }

    [Fact]
    public void ParsesMultipleResponsesFromOneAppend()
    {
        var first = MakeResponse(Pm3CommandCode.Version, new byte[] { 1 });
        var second = MakeResponse(Pm3CommandCode.Ping, new byte[] { 2, 3 });
        var combined = first.Concat(second).ToArray();
        var commands = new List<ushort>();
        var parser = new Pm3NgResponseStreamParser();
        parser.ResponseReceived += response => commands.Add(response.Command);

        parser.Append(combined);

        Assert.Equal(new[] { Pm3CommandCode.Version, Pm3CommandCode.Ping }, commands);
    }

    [Fact]
    public void ResynchronisesAfterGarbage()
    {
        var frame = MakeResponse(Pm3CommandCode.Version, Array.Empty<byte>());
        var input = new byte[] { 0xAA, 0xBB, 0xCC }.Concat(frame).ToArray();
        var parser = new Pm3NgResponseStreamParser();
        var count = 0;
        parser.ResponseReceived += _ => count++;

        parser.Append(input);

        Assert.Equal(1, count);
    }

    private static byte[] MakeResponse(ushort command, byte[] payload)
    {
        var frame = new byte[Pm3NgFrame.ResponseHeaderSize + payload.Length + Pm3NgFrame.PostambleSize];
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(0, 4), Pm3NgFrame.ResponseMagic);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4, 2), (ushort)(0x8000 | payload.Length));
        frame[6] = 0;
        frame[7] = 0;
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(8, 2), command);
        payload.CopyTo(frame.AsSpan(Pm3NgFrame.ResponseHeaderSize));
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(frame.Length - 2), Pm3NgFrame.ResponsePostambleMagic);
        return frame;
    }
}
