using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm3ResponseLedgerTests
{
    [Fact]
    public void MatchingResponseIsCorrelatedAndTimed()
    {
        var ledger = new Pm3ResponseLedger();
        var sent = DateTimeOffset.Parse("2026-09-19T10:00:00Z");
        var received = sent.AddMilliseconds(42);
        ledger.RegisterRequest(Pm3CommandCode.Version, sent);
        var response = new Pm3NgResponse(Pm3CommandCode.Version, 0, 0, Array.Empty<byte>(), Array.Empty<byte>());

        var entry = ledger.Observe(response, received);

        Assert.Equal(Pm3ResponseDisposition.Matched, entry.Disposition);
        Assert.Equal(TimeSpan.FromMilliseconds(42), entry.Age);
    }

    [Fact]
    public void WrongCommandIsRecordedWithoutConsumingPendingRequest()
    {
        var ledger = new Pm3ResponseLedger();
        var sent = DateTimeOffset.Parse("2026-09-19T10:00:00Z");
        ledger.RegisterRequest(Pm3CommandCode.Version, sent);
        var wrong = new Pm3NgResponse(0x7777, 0, 0, Array.Empty<byte>(), Array.Empty<byte>());

        var entry = ledger.Observe(wrong, sent.AddMilliseconds(10));

        Assert.Equal(Pm3ResponseDisposition.WrongCommand, entry.Disposition);
        Assert.True(ledger.ExpireOldest(out var command, out var originalSent));
        Assert.Equal(Pm3CommandCode.Version, command);
        Assert.Equal(sent, originalSent);
    }

    [Fact]
    public void LateResponseCanBeRecordedAfterRequestExpired()
    {
        var ledger = new Pm3ResponseLedger();
        var sent = DateTimeOffset.Parse("2026-09-19T10:00:00Z");
        ledger.RegisterRequest(Pm3CommandCode.Version, sent);
        Assert.True(ledger.ExpireOldest(out _, out _));

        var response = new Pm3NgResponse(Pm3CommandCode.Version, 0, 0, Array.Empty<byte>(), Array.Empty<byte>());
        var entry = ledger.Observe(response, sent.AddSeconds(2));

        Assert.Equal(Pm3ResponseDisposition.Late, entry.Disposition);
        Assert.Equal(TimeSpan.FromSeconds(2), entry.Age);
    }
}
