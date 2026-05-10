using System.Text;
using System.Text.Json;
using RunbookLens.Models;

namespace RunbookLens.Services;

public sealed class ReportExporter
{
    public string ExportMarkdown(string outputPath, ScanSummary summary, IEnumerable<LogHit> selectedHits, string operatorNotes)
    {
        var hits = selectedHits.ToList();
        var sb = new StringBuilder();
        sb.AppendLine("# Runbook Lens Incident Brief");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine($"Files scanned: {summary.FilesScanned}");
        sb.AppendLine($"Lines scanned: {summary.LinesScanned}");
        sb.AppendLine($"Signals selected: {hits.Count}");
        sb.AppendLine();
        sb.AppendLine("## Operator notes");
        sb.AppendLine(string.IsNullOrWhiteSpace(operatorNotes) ? "- No notes entered." : operatorNotes.Trim());
        sb.AppendLine();
        sb.AppendLine("## Severity counts");
        foreach (var group in summary.Hits.GroupBy(h => h.Severity).OrderBy(g => g.Key))
            sb.AppendLine($"- {group.Key}: {group.Count()}");
        sb.AppendLine();
        sb.AppendLine("## Selected evidence");
        foreach (var hit in hits)
            sb.AppendLine($"- {hit.Severity} / {hit.RuleName} / {Path.GetFileName(hit.FilePath)}:{hit.LineNumber} — {hit.Preview}");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        return outputPath;
    }

    public string SaveSession(string outputPath, ScanSummary summary)
    {
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return outputPath;
    }
}
