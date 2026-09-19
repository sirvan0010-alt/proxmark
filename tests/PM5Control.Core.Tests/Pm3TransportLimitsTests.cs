using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm3TransportLimitsTests
{
    [Theory]
    [InlineData(Pm3TransportKind.Pm5Usb, 4064)]
    [InlineData(Pm3TransportKind.Legacy, 512)]
    [InlineData(Pm3TransportKind.BwmFpc, 2048)]
    public void TransportLimitMatchesUpstreamBoundary(Pm3TransportKind transport, int expected)
        => Assert.Equal(expected, Pm3TransportLimits.MaxPayload(transport));

    [Fact]
    public void BwmFpcAccepts2048ButNot2049()
    {
        Assert.True(Pm3TransportLimits.Fits(Pm3TransportKind.BwmFpc, 2048));
        Assert.False(Pm3TransportLimits.Fits(Pm3TransportKind.BwmFpc, 2049));
    }
}
