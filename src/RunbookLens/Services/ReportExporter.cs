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
        sb.AppendLine("# Runbook Lens 점검 보고서");
        sb.AppendLine();
        sb.AppendLine($"생성 시각: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine($"스캔 파일 수: {summary.FilesScanned}");
        sb.AppendLine($"스캔 라인 수: {summary.LinesScanned}");
        sb.AppendLine($"선택 결과 수: {hits.Count}");
        sb.AppendLine();
        sb.AppendLine("## 운영 메모");
        sb.AppendLine(string.IsNullOrWhiteSpace(operatorNotes) ? "- 입력된 메모 없음." : operatorNotes.Trim());
        sb.AppendLine();
        sb.AppendLine("## 심각도별 집계");
        foreach (var group in summary.Hits.GroupBy(h => h.Severity).OrderBy(g => g.Key))
            sb.AppendLine($"- {group.Key}: {group.Count()}");
        sb.AppendLine();
        sb.AppendLine("## 선택한 증거");
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
