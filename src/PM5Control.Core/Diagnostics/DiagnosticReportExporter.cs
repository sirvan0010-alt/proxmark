/*
 * PM5 Control Center
 * PURPOSE: Export DiagnosticReport without losing evidence metadata.
 * RULE: Serialization is lossless with respect to source state and evidence;
 *       it does not infer or enrich hardware facts.
 * SEE: DiagnosticReport.cs, docs/DIAGNOSTIC_SCHEMA.json
 */

using System.Text.Json;
using System.Text.Json.Serialization;

namespace PM5Control.Core.Diagnostics;

public static class DiagnosticReportExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ToJson(DiagnosticReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, Options);
    }

    public static void WriteJson(DiagnosticReport report, string path)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllText(path, ToJson(report));
    }
}
