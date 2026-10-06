/*
 * PM5 Control Center
 *
 * PURPOSE: Decide which Windows serial port to use as the default candidate
 *          when more than one COM port is present, without silently hiding
 *          the other candidates from the user.
 * WHY:     The previous behaviour picked "COM3, or otherwise the first port"
 *          and never surfaced that other ports existed. On a machine with
 *          more than one serial device this can connect to the wrong port
 *          without the user ever knowing an alternative was available.
 * RULE:    This class never claims a port IS the PM5. It only orders
 *          candidates and explains why one was preferred. Identity must
 *          still come from the read-only protocol handshake.
 */

namespace PM5Control.Core.Connections;

/// <summary>
/// Result of selecting a default serial port among the ports currently
/// reported by Windows. This is a UX/ordering decision only: it carries no
/// claim about hardware identity.
/// </summary>
public sealed class SerialPortSelection
{
    public IReadOnlyList<string> Candidates { get; }
    public string? DefaultPort { get; }
    public bool IsAmbiguous => Candidates.Count > 1;
    public string Reason { get; }

    private SerialPortSelection(IReadOnlyList<string> candidates, string? defaultPort, string reason)
    {
        Candidates = candidates;
        DefaultPort = defaultPort;
        Reason = reason;
    }

    /// <summary>
    /// Chooses a default port from the detected candidates. A previously
    /// selected port is preferred only when it is still present.
    /// </summary>
    public static SerialPortSelection Choose(IEnumerable<string> detectedPorts, string? preferredPort = null)
    {
        ArgumentNullException.ThrowIfNull(detectedPorts);

        var ordered = detectedPorts
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(ComPortSortKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ordered.Length == 0)
            return new SerialPortSelection(Array.Empty<string>(), null, "No Windows serial port was detected.");

        if (!string.IsNullOrWhiteSpace(preferredPort))
        {
            var kept = ordered.FirstOrDefault(p => p.Equals(preferredPort, StringComparison.OrdinalIgnoreCase));
            if (kept is not null)
            {
                var keptReason = ordered.Length == 1
                    ? $"Keeping previously selected port {kept} (only port detected)."
                    : $"Keeping previously selected port {kept} (still present among {ordered.Length} detected ports).";
                return new SerialPortSelection(ordered, kept, keptReason);
            }
        }

        if (ordered.Length == 1)
            return new SerialPortSelection(ordered, ordered[0], $"Only one serial port detected: {ordered[0]}.");

        var reason = $"{ordered.Length} serial ports detected ({string.Join(", ", ordered)}). " +
                     $"Defaulting to {ordered[0]}; this is a guess, not a verified PM5 identity. " +
                     "Use the port selector if this is the wrong device.";
        return new SerialPortSelection(ordered, ordered[0], reason);
    }

    private static string ComPortSortKey(string portName)
    {
        if (portName.Length > 3 &&
            portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(portName.AsSpan(3), out var number))
        {
            return number.ToString("D10");
        }

        return portName;
    }
}
