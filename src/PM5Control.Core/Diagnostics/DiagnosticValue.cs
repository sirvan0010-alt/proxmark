/*
 * PM5 Control Center
 *
 * PURPOSE: Represents a diagnostic value together with its evidence.
 * WHY: A Proxmark5 value must never be presented as verified when it is
 *      merely reported by firmware or inferred from compatibility data.
 * RULE: Preserve DETECTED / REPORTED / EXPECTED / UNKNOWN explicitly.
 * SEE: README.md, AI_CONTEXT.md, docs/ARCHITECTURE.md
 */

namespace PM5Control.Core.Diagnostics;

public enum DiagnosticSourceState
{
    Detected,
    Reported,
    Expected,
    Unknown
}

public enum DiagnosticConfidence
{
    High,
    Medium,
    Low,
    Unknown
}

public sealed record DiagnosticValue<T>
{
    public T Value { get; }
    public bool HasValue { get; }
    public DiagnosticSourceState SourceState { get; }
    public DiagnosticConfidence Confidence { get; }
    public string SourceDescription { get; }
    public DateTimeOffset Timestamp { get; }

    public DiagnosticValue(
        T value,
        DiagnosticSourceState sourceState,
        DiagnosticConfidence confidence,
        string sourceDescription,
        DateTimeOffset timestamp)
        : this(value, value is not null, sourceState, confidence, sourceDescription, timestamp)
    {
    }

    public DiagnosticValue(
        T value,
        bool hasValue,
        DiagnosticSourceState sourceState,
        DiagnosticConfidence confidence,
        string sourceDescription,
        DateTimeOffset timestamp)
    {
        Value = value;
        HasValue = hasValue;
        SourceState = sourceState;
        Confidence = confidence;
        SourceDescription = sourceDescription;
        Timestamp = timestamp;
    }

    public static DiagnosticValue<T> Unknown(string reason) =>
        new(default!, false, DiagnosticSourceState.Unknown, DiagnosticConfidence.Unknown, reason, DateTimeOffset.UtcNow);
}
