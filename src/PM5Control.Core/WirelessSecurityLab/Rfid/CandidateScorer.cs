using System;
using System.Collections.Generic;
using System.Linq;

namespace PM5Control.Core.WirelessSecurityLab.Rfid
{
    public sealed class CandidateScorer
    {
        private readonly ChecksumAnalyzer _checksumAnalyzer = new();

        public List<UidCandidate> ScoreCandidates(IEnumerable<UidCandidate> candidates, PatternReport report)
        {
            if (candidates is null) throw new ArgumentNullException(nameof(candidates));
            if (report is null) throw new ArgumentNullException(nameof(report));

            var list = candidates.ToList();
            foreach (var cand in list)
            {
                double score = 0.0;

                if (report.CommonPrefix is { Length: > 0 } && cand.Uid.Length >= report.CommonPrefix.Length)
                {
                    int match = 0;
                    for (int i = 0; i < report.CommonPrefix.Length; i++)
                        if (cand.Uid[i] == report.CommonPrefix[i]) match++;
                    score += (double)match / report.CommonPrefix.Length * 0.4;
                }

                if (report.ByteEntropy.Count > 0)
                {
                    double avgEntropy = report.ByteEntropy.Values.Average();
                    double candEntropy = 0.0;
                    var freq = cand.Uid.GroupBy(b => b).ToDictionary(g => g.Key, g => g.Count());
                    foreach (var count in freq.Values)
                    {
                        double p = (double)count / cand.Uid.Length;
                        candEntropy -= p * Math.Log2(p);
                    }

                    if (candEntropy < avgEntropy)
                        score += 0.3 * (1 - candEntropy / (avgEntropy + 0.01));
                }

                var detected = _checksumAnalyzer.DetectChecksumType(cand.Uid);
                if (detected == report.DetectedChecksum && report.DetectedChecksum != ChecksumType.None)
                    score += 0.3;

                cand.Score = Math.Round(Math.Clamp(score, 0, 1), 3);
            }

            return list.OrderByDescending(c => c.Score).ToList();
        }
    }
}
