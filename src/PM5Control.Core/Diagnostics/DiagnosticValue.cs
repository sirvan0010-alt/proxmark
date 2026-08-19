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

public sealed record DiagnosticValue<T>(
    T? Value,
    DiagnosticSourceState SourceState,
    DiagnosticConfidence Confidence,
    string SourceDescription,
    DateTimeOffset Timestamp);
