using System;
using System.Threading;
using System.Threading.Tasks;

namespace PM5Control.Core.WirelessLab.WiFi;

public sealed record WifiTcpHealthResult(bool Connected, string Host, int Port, TimeSpan Elapsed, string EvidenceLevel);

public static class WifiTcpDiagnostics
{
    public static async Task<WifiTcpHealthResult> ProbeAsync(string host, int port = 7901, int timeoutMs = 3000, CancellationToken ct = default)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        await using var transport = new WifiTcpTransport(host, port, timeoutMs);
        var connected = await transport.OpenAsync(ct).ConfigureAwait(false);
        return new WifiTcpHealthResult(connected, host, port, started.Elapsed, connected ? "L1_TCP_REACHABLE" : "L0_UNREACHABLE");
    }
}