/*
 * PM5 Control Center
 * PURPOSE: Machine-readable diagnostic report with an explicit evidence chain.
 * RULE: A report may contain unknown/simulated values but must not promote
 *       them to detected hardware facts.
 * SEE: docs/DIAGNOSTIC_SCHEMA.md, docs/DIAGNOSTIC_SCHEMA.json, AI_CONTEXT.md
 */

namespace PM5Control.Core.Diagnostics;

public sealed record DiagnosticEvidence(
    DateTimeOffset Timestamp,
    string Probe,
    string? Transport,
    string? Request,
    string? Response,
    string? ParsedResult,
    double? LatencyMs,
    int Retries,
    string Source,
    DiagnosticConfidence Confidence,
    string SoftwareCommit);

public sealed record DiagnosticReport(
    DateTimeOffset CreatedAt,
    string ToolVersion,
    string? SoftwareCommit,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyList<DiagnosticEvidence> Evidence);
