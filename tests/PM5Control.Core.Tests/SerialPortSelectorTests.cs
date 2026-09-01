using PM5Control.Core.Connections;

namespace PM5Control.Core.Tests;

public sealed class SerialPortSelectorTests
{
    [Fact]
    public void Choose_NoPorts_ReturnsNullDefaultAndEmptyCandidates()
    {
        var result = SerialPortSelection.Choose(Array.Empty<string>());

        Assert.Null(result.DefaultPort);
        Assert.Empty(result.Candidates);
        Assert.False(result.IsAmbiguous);
        Assert.Contains("No Windows serial port", result.Reason);
    }

    [Fact]
    public void Choose_SinglePort_IsSelectedAndNotAmbiguous()
    {
        var result = SerialPortSelection.Choose(new[] { "COM7" });

        Assert.Equal("COM7", result.DefaultPort);
        Assert.False(result.IsAmbiguous);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public void Choose_MultiplePorts_OrdersNumericallyAndFlagsAmbiguous()
    {
        var result = SerialPortSelection.Choose(new[] { "COM10", "COM2", "COM3" });

        Assert.True(result.IsAmbiguous);
        Assert.Equal(new[] { "COM2", "COM3", "COM10" }, result.Candidates);
        Assert.Equal("COM2", result.DefaultPort);
        Assert.Contains("3 serial ports detected", result.Reason);
    }

    [Fact]
    public void Choose_DuplicatePorts_AreDeduplicatedCaseInsensitively()
    {
        var result = SerialPortSelection.Choose(new[] { "COM4", "com4", "COM4" });

        Assert.Single(result.Candidates);
        Assert.False(result.IsAmbiguous);
    }

    [Fact]
    public void Choose_PreferredPortStillPresent_IsKeptOverDefaultOrdering()
    {
        var result = SerialPortSelection.Choose(new[] { "COM2", "COM9" }, preferredPort: "COM9");

        Assert.Equal("COM9", result.DefaultPort);
        Assert.Contains("Keeping previously selected port COM9", result.Reason);
    }

    [Fact]
    public void Choose_PreferredPortNoLongerPresent_FallsBackToOrderedDefault()
    {
        var result = SerialPortSelection.Choose(new[] { "COM2", "COM9" }, preferredPort: "COM5");

        Assert.Equal("COM2", result.DefaultPort);
    }

    [Fact]
    public void Choose_BlankAndNullEntries_AreIgnored()
    {
        var result = SerialPortSelection.Choose(new[] { "COM5", "", "  ", null! });

        Assert.Single(result.Candidates);
        Assert.Equal("COM5", result.DefaultPort);
    }

    [Fact]
    public void Choose_NonStandardPortNames_SortOrdinallyWithoutThrowing()
    {
        var result = SerialPortSelection.Choose(new[] { "/dev/ttyUSB1", "/dev/ttyUSB0" });

        Assert.Equal(new[] { "/dev/ttyUSB0", "/dev/ttyUSB1" }, result.Candidates);
        Assert.Equal("/dev/ttyUSB0", result.DefaultPort);
    }
}
