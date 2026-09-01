using System;
using System.Collections.Generic;

namespace PM5Control.Core.WirelessSecurityLab.Rfid
{
    public sealed class EvidenceLog
    {
        private readonly List<EvidenceEntry> _entries = new();
        private readonly object _lock = new();

        public void LogGeneration(UidCandidate candidate, FuzzingStrategy strategy)
        {
            lock (_lock)
            {
                _entries.Add(new EvidenceEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Candidate = candidate,
                    Strategy = strategy,
                    EventType = "Generation"
                });
            }
        }

        public void LogAnalysis(PatternReport report)
        {
            lock (_lock)
            {
                string prefixHex = report.CommonPrefix is null
                    ? "(none)"
                    : Convert.ToHexString(report.CommonPrefix);

                _entries.Add(new EvidenceEntry
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = "Analysis",
                    Details =
                        $"prefix={prefixHex}, checksum={report.DetectedChecksum}, " +
                        $"sequential={report.SequentialCorrelationDetected}, " +
                        $"prefixConfidence={report.PrefixConfidence:F2}"
                });
            }
        }

        public void LogExplicitEmulation(byte[] uid, string mode)
        {
            lock (_lock)
            {
                _entries.Add(new EvidenceEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Candidate = new UidCandidate { Uid = uid },
                    EventType = "ExplicitEmulation",
                    Details = $"Mode: {mode}"
                });
            }
        }

        public IReadOnlyList<EvidenceEntry> GetEvidence()
        {
            lock (_lock) return _entries.AsReadOnly();
        }

        public void Clear()
        {
            lock (_lock) _entries.Clear();
        }
    }

    public sealed record EvidenceEntry
    {
        public DateTime Timestamp { get; init; }
        public UidCandidate? Candidate { get; init; }
        public FuzzingStrategy Strategy { get; init; }
        public string EventType { get; init; } = string.Empty;
        public string? Details { get; init; }
    }
}
