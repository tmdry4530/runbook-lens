namespace RunbookLens.Models;

public sealed record SignalRule(string Name, string Pattern, string Severity, string Description)
{
    public override string ToString() => $"[{Severity}] {Name}";
}

public sealed record LogHit(
    string FilePath,
    int LineNumber,
    string Severity,
    string RuleName,
    string Preview,
    DateTimeOffset? Timestamp);

public sealed record ScanOptions(int MaxFiles, long MaxFileBytes)
{
    public static ScanOptions Default { get; } = new(MaxFiles: 500, MaxFileBytes: 25 * 1024 * 1024);
}

public sealed record SkippedFile(string FilePath, string Reason);

public sealed record SeverityCount(string Severity, int Count);

public sealed class TriageNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public string Title { get; set; } = "Untitled incident";
    public string Summary { get; set; } = string.Empty;
    public List<LogHit> Evidence { get; set; } = new();
    public List<string> Checklist { get; set; } = new();
}

public sealed class ScanSummary
{
    private static readonly string[] SeverityOrder = ["심각", "높음", "중간", "낮음"];

    public int FilesScanned { get; set; }
    public int FilesSkipped => SkippedFiles.Count;
    public int LinesScanned { get; set; }
    public TimeSpan Duration { get; set; }
    public List<LogHit> Hits { get; set; } = new();
    public List<SkippedFile> SkippedFiles { get; set; } = new();

    public IEnumerable<SeverityCount> GetSeverityCounts()
    {
        return Hits
            .GroupBy(hit => hit.Severity)
            .Select(group => new SeverityCount(group.Key, group.Count()))
            .OrderBy(item => Array.IndexOf(SeverityOrder, item.Severity) is var index && index >= 0 ? index : int.MaxValue)
            .ThenBy(item => item.Severity);
    }
}
