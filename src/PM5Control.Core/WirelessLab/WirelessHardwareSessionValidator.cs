using System;
using System.Collections.Generic;

namespace PM5Control.Core.WirelessLab;

public sealed class HardwareValidationSession
{
    public string SessionId { get; init; } = "";
    public string DeviceIdentity { get; init; } = "";
    public string Technology { get; init; } = "";
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; internal set; }
    public bool HumanConfirmed { get; internal set; }
}

public sealed class CapabilityValidation
{
    public byte CapabilityId { get; init; }
    public EvidenceLevel PreviousEvidence { get; init; }
    public EvidenceLevel NewEvidence { get; init; }
    public bool EvidenceMatchedSession { get; init; }
    public string Reason { get; init; } = "";
}

public sealed class HardwareSessionResult
{
    public HardwareValidationSession Session { get; init; } = null!;
    public IReadOnlyList<CapabilityValidation> Validations { get; init; } = Array.Empty<CapabilityValidation>();
    public bool Completed { get; init; }
}

/// <summary>Human-confirmed, session-bound gate for promoting test evidence.</summary>
public sealed class WirelessHardwareSessionValidator
{
    private readonly Dictionary<string, HardwareValidationSession> _sessions = new();
    public IReadOnlyCollection<HardwareValidationSession> Sessions => _sessions.Values;

    public HardwareValidationSession StartSession(string deviceIdentity, string technology, string humanConfirmationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceIdentity)) throw new ArgumentException("Device identity is required.", nameof(deviceIdentity));
        if (string.IsNullOrWhiteSpace(technology)) throw new ArgumentException("Technology is required.", nameof(technology));
        if (string.IsNullOrWhiteSpace(humanConfirmationToken)) throw new InvalidOperationException("A human confirmation token is required to start a hardware session.");
        var session = new HardwareValidationSession { SessionId = Guid.NewGuid().ToString("N"), DeviceIdentity = deviceIdentity, Technology = technology, StartedAt = DateTime.UtcNow, HumanConfirmed = true };
        _sessions.Add(session.SessionId, session);
        return session;
    }

    public HardwareSessionResult CompleteSession(CapabilityMatrixBase matrix, string sessionId, string deviceIdentity)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) throw new ArgumentException("Unknown validation session.", nameof(sessionId));
        if (!session.HumanConfirmed) throw new InvalidOperationException("Session is not human-confirmed.");
        if (!string.Equals(session.DeviceIdentity, deviceIdentity, StringComparison.Ordinal)) throw new InvalidOperationException("Device identity does not match the session binding.");

        var validations = new List<CapabilityValidation>();
        foreach (var cap in matrix.GetAll())
        {
            var previous = cap.Evidence;
            bool matched = string.Equals(cap.LastSessionId, sessionId, StringComparison.Ordinal) && cap.LastTestedAt is not null && cap.LastTestedAt >= session.StartedAt;
            if (!matched) { validations.Add(new CapabilityValidation { CapabilityId = cap.Id, PreviousEvidence = previous, NewEvidence = previous, Reason = "No matching test evidence from this session." }); continue; }
            if (cap.Policy == PolicyStatus.Disabled) { validations.Add(new CapabilityValidation { CapabilityId = cap.Id, PreviousEvidence = previous, NewEvidence = previous, EvidenceMatchedSession = true, Reason = "Policy disabled; test is recorded but cannot promote evidence." }); continue; }
            if (cap.Support == SupportStatus.NotSupported) { validations.Add(new CapabilityValidation { CapabilityId = cap.Id, PreviousEvidence = previous, NewEvidence = previous, EvidenceMatchedSession = true, Reason = "Documentation says not supported; requires human support review despite any observed result." }); continue; }
            var next = cap.LastTestResult == CapabilityTestResult.Pass ? EvidenceLevel.HardwareVerified : cap.LastTestResult == CapabilityTestResult.Fail ? EvidenceLevel.HardwareContradicted : EvidenceLevel.Tested;
            if (next == EvidenceLevel.HardwareVerified && cap.Support != SupportStatus.Supported) next = EvidenceLevel.Tested;
            cap.Evidence = next; cap.RecomputeExposure();
            validations.Add(new CapabilityValidation { CapabilityId = cap.Id, PreviousEvidence = previous, NewEvidence = next, EvidenceMatchedSession = true, Reason = $"Evidence matched session {sessionId}." });
        }
        session.CompletedAt = DateTime.UtcNow;
        return new HardwareSessionResult { Session = session, Validations = validations, Completed = true };
    }
}
