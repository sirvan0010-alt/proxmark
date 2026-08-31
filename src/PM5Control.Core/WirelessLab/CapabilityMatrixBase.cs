using System;
using System.Collections.Generic;

namespace PM5Control.Core.WirelessLab;

public abstract class CapabilityMatrixBase
{
    protected readonly Dictionary<byte, WirelessCapability> _capabilities = new();
    public IReadOnlyCollection<WirelessCapability> GetAll() => _capabilities.Values;
    public WirelessCapability? GetCapability(byte id) => _capabilities.TryGetValue(id, out var c) ? c : null;
    public bool IsHardwareVerified(byte id) => GetCapability(id)?.Evidence == EvidenceLevel.HardwareVerified;
    public bool IsHardwareContradicted(byte id) => GetCapability(id)?.Evidence == EvidenceLevel.HardwareContradicted;
    public bool IsPolicyDisabled(byte id) => GetCapability(id)?.Policy == PolicyStatus.Disabled;
    public bool IsNotSupported(byte id) => GetCapability(id)?.Support == SupportStatus.NotSupported;

    public void RecordTestResult(byte capId, CapabilityTestResult result, string sessionId, byte? errorCode = null,
        string? rawResponseHex = null, string? testCommandHex = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("Session ID is required.", nameof(sessionId));
        if (!_capabilities.TryGetValue(capId, out var cap)) throw new ArgumentException($"Capability 0x{capId:X2} not registered", nameof(capId));
        cap.TestHistory.Add(new HardwareTestRecord { CapabilityId = capId, Result = result, SessionId = sessionId, ErrorCode = errorCode, Timestamp = DateTime.UtcNow, RawResponseHex = rawResponseHex, TestCommandHex = testCommandHex });
        if (cap.Policy == PolicyStatus.Disabled || cap.Support == SupportStatus.NotSupported) { cap.RecomputeExposure(); return; }
        cap.Evidence = result switch
        {
            CapabilityTestResult.Pass => EvidenceLevel.HardwareVerified,
            CapabilityTestResult.Fail => EvidenceLevel.HardwareContradicted,
            CapabilityTestResult.Error or CapabilityTestResult.Timeout => EvidenceLevel.Tested,
            _ => cap.Evidence
        };
        cap.RecomputeExposure();
    }

    public void HumanPolicyReview(byte capId, PolicyStatus newPolicy, string authorizationToken, string? notes = null)
    {
        RequireToken(authorizationToken, "Policy review");
        var cap = Require(capId);
        cap.Policy = newPolicy;
        cap.RecomputeExposure();
    }

    public void HumanSupportReview(byte capId, SupportStatus newSupport, string authorizationToken, string? sourceReference = null)
    {
        RequireToken(authorizationToken, "Support review");
        var cap = Require(capId);
        cap.Support = newSupport;
        if (!string.IsNullOrWhiteSpace(sourceReference)) cap.SourceReference = sourceReference;
        if (newSupport == SupportStatus.NotSupported && cap.Evidence == EvidenceLevel.HardwareVerified)
            cap.Evidence = EvidenceLevel.HardwareContradicted;
        cap.RecomputeExposure();
    }

    protected WirelessCapability Require(byte id) => _capabilities.TryGetValue(id, out var cap) ? cap : throw new ArgumentException($"Capability 0x{id:X2} not registered", nameof(id));
    protected static void RequireToken(string token, string operation)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException($"{operation} requires non-empty human authorization token.");
    }
    public void RegisterDocumentedSupported(byte id, string name, string description, WirelessCapabilityCategory category, string sourceReference) => Register(id, name, description, EvidenceLevel.Documented, SupportStatus.Supported, PolicyStatus.Allowed, category, sourceReference);
    public void RegisterDocumentedNotSupported(byte id, string name, string description, WirelessCapabilityCategory category, string sourceReference) => Register(id, name, description, EvidenceLevel.Documented, SupportStatus.NotSupported, PolicyStatus.Disabled, category, sourceReference);
    public void RegisterDocumentedPolicyDisabled(byte id, string name, string description, WirelessCapabilityCategory category, string policyReason, string sourceReference) => Register(id, name, $"{description} [POLICY: {policyReason}]", EvidenceLevel.Documented, SupportStatus.Supported, PolicyStatus.Disabled, category, sourceReference);
    public void RegisterUnknown(byte id, string name, string description, WirelessCapabilityCategory category) => Register(id, name, description, EvidenceLevel.Unknown, SupportStatus.Unknown, PolicyStatus.Allowed, category, null);
    private void Register(byte id, string name, string description, EvidenceLevel evidence, SupportStatus support, PolicyStatus policy, WirelessCapabilityCategory category, string? source)
    {
        _capabilities[id] = new WirelessCapability { Id = id, Name = name, Description = description, Evidence = evidence, Support = support, Policy = policy, Category = category, SourceReference = source, Exposure = UIExposure.Hidden };
    }
}
