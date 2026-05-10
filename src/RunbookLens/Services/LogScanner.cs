using System.Text.RegularExpressions;
using RunbookLens.Models;

namespace RunbookLens.Services;

public sealed class LogScanner
{
    private static readonly string[] DefaultExtensions = [".log", ".txt", ".json", ".csv", ".md", ".out", ".trace"];

    public IReadOnlyList<SignalRule> DefaultRules { get; } = new List<SignalRule>
    {
        new("Unhandled exception", "exception|stack trace|NullReferenceException|InvalidOperationException", "Critical", "Crashes and unhandled code paths"),
        new("Timeout or latency", "timeout|timed out|latency|slow query|deadline exceeded", "High", "SLA and dependency latency indicators"),
        new("Auth or permission", "unauthorized|forbidden|permission denied|401|403|invalid token", "High", "Login and access-control failures"),
        new("Data loss risk", "corrupt|failed to write|disk full|out of space|cannot save", "Critical", "Persistence and local data safety issues"),
        new("Payment or order risk", "payment failed|refund|chargeback|order failed|invoice", "High", "SMB revenue workflow risks"),
        new("Generic error", "\berror\b|\bfail(?:ed|ure)?\b|fatal", "Medium", "Broad error and failure wording")
    };

    public async Task<ScanSummary> ScanAsync(string rootPath, IEnumerable<SignalRule> rules, int maxFiles, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.Now;
        var compiledRules = rules
            .Select(rule => (Rule: rule, Regex: new Regex(rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)))
            .ToList();
        var files = EnumerateCandidateFiles(rootPath).Take(Math.Max(1, maxFiles)).ToList();
        var summary = new ScanSummary { FilesScanned = files.Count };

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int lineNo = 0;
            try
            {
                using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                while (!reader.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;
                    lineNo++;
                    summary.LinesScanned++;
                    foreach (var item in compiledRules)
                    {
                        if (!item.Regex.IsMatch(line)) continue;
                        summary.Hits.Add(new LogHit(file, lineNo, item.Rule.Severity, item.Rule.Name, Trim(line, 240), TryExtractTimestamp(line)));
                    }
                }
            }
            catch (IOException ex)
            {
                summary.Hits.Add(new LogHit(file, lineNo, "Low", "Read warning", ex.Message, null));
            }
            catch (UnauthorizedAccessException ex)
            {
                summary.Hits.Add(new LogHit(file, lineNo, "Low", "Access warning", ex.Message, null));
            }
        }

        summary.Duration = DateTimeOffset.Now - started;
        return summary;
    }

    private static IEnumerable<string> EnumerateCandidateFiles(string rootPath)
    {
        if (File.Exists(rootPath)) return [rootPath];
        if (!Directory.Exists(rootPath)) return [];
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System | FileAttributes.Temporary
        };
        return Directory.EnumerateFiles(rootPath, "*.*", options)
            .Where(path => DefaultExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(GetLastWriteTimeSafe);
    }

    private static DateTime GetLastWriteTimeSafe(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch (IOException)
        {
            return DateTime.MinValue;
        }
        catch (UnauthorizedAccessException)
        {
            return DateTime.MinValue;
        }
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max] + "…";

    private static DateTimeOffset? TryExtractTimestamp(string line)
    {
        var match = Regex.Match(line, @"\b\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?\b");
        return match.Success && DateTimeOffset.TryParse(match.Value, out var parsed) ? parsed : null;
    }
}
