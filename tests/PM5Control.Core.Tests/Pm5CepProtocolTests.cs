using PM5Control.Core.Protocols.Cep;

namespace PM5Control.Core.Tests;

public sealed class Pm5CepProtocolTests
{
    [Fact]
    public void HandshakeProbeMatchesUpstreamShape()
    {
        var probe = Pm5CepProtocol.BuildHandshakeProbe();

        Assert.Equal(0x02, probe[0]);
        Assert.Equal("iamf0rupm5"u8.ToArray(), probe[1..]);
    }

    [Fact]
    public void HandshakeReplyIsRecognised()
    {
        Assert.True(Pm5CepProtocol.IsHandshakeResponse("yes"u8));
        Assert.False(Pm5CepProtocol.IsHandshakeResponse("no"u8));
    }

    [Fact]
    public void LengthPrefixIsLittleEndian()
    {
        Assert.True(Pm5CepProtocol.TryReadLength(new byte[] { 0x34, 0x12 }, out var length));
        Assert.Equal((ushort)0x1234, length);
    }

    [Fact]
    public void ShortLengthPrefixIsRejected()
    {
        Assert.False(Pm5CepProtocol.TryReadLength(new byte[] { 0x01 }, out _));
    }
}
