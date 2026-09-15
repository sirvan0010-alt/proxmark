using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm3AbortContractTests
{
    [Fact]
    public void BreakLoopUsesVerifiedUpstreamCommandId()
    {
        Assert.Equal((ushort)0x0118, Pm3CommandCode.BreakLoop);
        Assert.False(Pm3CommandCode.IsSafeReadOnlyProbe(Pm3CommandCode.BreakLoop));
    }

    [Fact]
    public void BreakLoopEncodesAsEmptyPayloadNgCommand()
    {
        var frame = Pm3NgFrame.EncodeCommand(Pm3CommandCode.BreakLoop);

        Assert.Equal(Convert.FromHexString("504D33610881113362"), frame);
    }
}
