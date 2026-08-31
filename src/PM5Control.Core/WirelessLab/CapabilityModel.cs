using System;

namespace PM5Control.Core.WirelessLab;

public enum EvidenceLevel { Documented, Tested, HardwareVerified, HardwareContradicted, Unknown }
public enum SupportStatus { Supported, NotSupported, Unknown }
public enum PolicyStatus { Allowed, RequiresAuth, Disabled }
public enum UIExposure { Hidden, Visible, AuthRequired }
public enum CapabilityTestResult { Pass, Fail, Error, Timeout, NotRun }
public enum WirelessCapabilityCategory { Connectivity, Scanning, Monitoring, FrameInjection, Security, PowerManagement, Informational }

public sealed class HardwareTestRecord
{
    public string SessionId { get; init; } = "";
    public byte CapabilityId { get; init; }
    public CapabilityTestResult Result { get; init; }
    public byte? ErrorCode { get; init; }
    public DateTime Timestamp { get; init; }
    public string? RawResponseHex { get; init; }
    public string? TestCommandHex { get; init; }
}

public sealed class WirelessCapability
{
    public byte Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public EvidenceLevel Evidence { get; internal set; } = EvidenceLevel.Unknown;
    public SupportStatus Support { get; internal set; } = SupportStatus.Unknown;
    public PolicyStatus Policy { get; internal set; } = PolicyStatus.Allowed;
    public UIExposure Exposure { get; internal set; } = UIExposure.Hidden;
    public WirelessCapabilityCategory Category { get; init; }
    public string? SourceReference { get; internal set; }
    public List<HardwareTestRecord> TestHistory { get; } = new();
    public CapabilityTestResult? LastTestResult => TestHistory.Count == 0 ? null : TestHistory[^1].Result;
    public DateTime? LastTestedAt => TestHistory.Count == 0 ? null : TestHistory[^1].Timestamp;
    public byte? LastErrorCode => TestHistory.Count == 0 ? null : TestHistory[^1].ErrorCode;
    public string? LastSessionId => TestHistory.Count == 0 ? null : TestHistory[^1].SessionId;

    public void RecomputeExposure()
    {
        Exposure = Support switch
        {
            SupportStatus.NotSupported => UIExposure.Hidden,
            _ when Policy == PolicyStatus.Disabled => UIExposure.Hidden,
            _ when Policy == PolicyStatus.RequiresAuth => UIExposure.AuthRequired,
            _ when Evidence == EvidenceLevel.HardwareVerified => UIExposure.Visible,
            _ => UIExposure.Hidden
        };
    }
}
