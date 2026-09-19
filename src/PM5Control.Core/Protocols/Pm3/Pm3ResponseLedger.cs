namespace PM5Control.Core.Protocols.Pm3;

public enum Pm3ResponseDisposition
{
    Matched,
    Late,
    WrongCommand,
    Unsolicited
}

public sealed record Pm3ResponseLedgerEntry(
    DateTimeOffset SentAt,
    ushort ExpectedCommand,
    ushort? ReceivedCommand,
    Pm3ResponseDisposition Disposition,
    TimeSpan? Age,
    Pm3NgResponse Response);

/// <summary>Tracks request/response correlation without discarding late or wrong responses.</summary>
public sealed class Pm3ResponseLedger
{
    private sealed record Pending(ushort Command, DateTimeOffset SentAt);
    private readonly Queue<Pending> _pending = new();
    private readonly List<Pm3ResponseLedgerEntry> _entries = new();

    public IReadOnlyList<Pm3ResponseLedgerEntry> Entries => _entries;

    public void RegisterRequest(ushort expectedCommand, DateTimeOffset sentAt)
        => _pending.Enqueue(new Pending(expectedCommand, sentAt));

    public Pm3ResponseLedgerEntry Observe(Pm3NgResponse response, DateTimeOffset receivedAt)
    {
        var pending = _pending.Count == 0 ? null : _pending.Peek();
        Pm3ResponseDisposition disposition;
        DateTimeOffset sentAt = receivedAt;
        TimeSpan? age = null;

        if (pending is null)
        {
            disposition = Pm3ResponseDisposition.Unsolicited;
        }
        else if (pending.Command == response.Command)
        {
            _pending.Dequeue();
            disposition = Pm3ResponseDisposition.Matched;
            sentAt = pending.SentAt;
            age = receivedAt - pending.SentAt;
        }
        else
        {
            disposition = Pm3ResponseDisposition.WrongCommand;
            sentAt = pending.SentAt;
            age = receivedAt - pending.SentAt;
        }

        var entry = new Pm3ResponseLedgerEntry(sentAt, pending?.Command ?? response.Command,
            response.Command, disposition, age, response);
        _entries.Add(entry);
        return entry;
    }

    /// <summary>Marks the oldest still-pending request as expired without fabricating a response.</summary>
    public bool ExpireOldest(out ushort expectedCommand, out DateTimeOffset sentAt)
    {
        if (_pending.Count == 0)
        {
            expectedCommand = 0;
            sentAt = default;
            return false;
        }

        var pending = _pending.Dequeue();
        expectedCommand = pending.Command;
        sentAt = pending.SentAt;
        return true;
    }
}
