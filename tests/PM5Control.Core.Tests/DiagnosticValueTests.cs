using PM5Control.Core.Diagnostics;

namespace PM5Control.Core.Tests;

public sealed class DiagnosticValueTests
{
    [Fact]
    public void ValueType_ValidValue_HasValueIsTrue()
    {
        var value = new DiagnosticValue<ushort>(0xDA10, DiagnosticSourceState.Reported, DiagnosticConfidence.Medium, "test", DateTimeOffset.UtcNow);

        Assert.True(value.HasValue);
        Assert.Equal((ushort)0xDA10, value.Value);
    }

    [Fact]
    public void ValueType_UnknownValue_HasValueIsFalse()
    {
        var value = new DiagnosticValue<ushort>(0, false, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown, "test", DateTimeOffset.UtcNow);

        Assert.False(value.HasValue);
        Assert.Equal((ushort)0, value.Value);
        Assert.Equal(DiagnosticSourceState.Unknown, value.SourceState);
    }

    [Fact]
    public void UnknownFactory_WorksForValueAndReferenceTypes()
    {
        var number = DiagnosticValue<long>.Unknown("missing");
        var text = DiagnosticValue<string>.Unknown("missing");

        Assert.False(number.HasValue);
        Assert.Equal(0L, number.Value);
        Assert.False(text.HasValue);
        Assert.Null(text.Value);
        Assert.Equal(DiagnosticSourceState.Unknown, number.SourceState);
        Assert.Equal(DiagnosticSourceState.Unknown, text.SourceState);
    }

    [Fact]
    public void ReferenceType_NullValue_HasValueIsFalse()
    {
        var value = new DiagnosticValue<string>(null, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown, "test", DateTimeOffset.UtcNow);

        Assert.False(value.HasValue);
        Assert.Null(value.Value);
    }

    [Fact]
    public void ReferenceType_RealValue_HasValueIsTrue()
    {
        var value = new DiagnosticValue<string>("BWM", DiagnosticSourceState.Reported, DiagnosticConfidence.Medium, "test", DateTimeOffset.UtcNow);

        Assert.True(value.HasValue);
        Assert.Equal("BWM", value.Value);
    }
}
