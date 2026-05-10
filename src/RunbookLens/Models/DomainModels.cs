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
    public int FilesScanned { get; set; }
    public int LinesScanned { get; set; }
    public TimeSpan Duration { get; set; }
    public List<LogHit> Hits { get; set; } = new();
}
