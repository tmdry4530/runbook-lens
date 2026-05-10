using System.Text.RegularExpressions;
using RunbookLens.Models;

namespace RunbookLens.Services;

public sealed class LogScanner
{
    private static readonly string[] DefaultExtensions = [".log", ".txt", ".json", ".csv", ".md", ".out", ".trace"];

    public IReadOnlyList<SignalRule> DefaultRules { get; } = new List<SignalRule>
    {
        new("처리되지 않은 예외", "exception|stack trace|NullReferenceException|InvalidOperationException", "심각", "크래시 및 처리되지 않은 코드 경로"),
        new("타임아웃/지연", "timeout|timed out|latency|slow query|deadline exceeded", "높음", "SLA 및 의존성 지연 징후"),
        new("인증/권한 문제", "unauthorized|forbidden|permission denied|401|403|invalid token", "높음", "로그인 및 접근 제어 실패"),
        new("데이터 손실 위험", "corrupt|failed to write|disk full|out of space|cannot save", "심각", "저장소 및 로컬 데이터 안전성 문제"),
        new("결제/주문 위험", "payment failed|refund|chargeback|order failed|invoice", "높음", "소규모 비즈니스 매출 흐름 위험"),
        new("일반 오류", "\berror\b|\bfail(?:ed|ure)?\b|fatal", "중간", "광범위한 오류/실패 문구")
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
                summary.Hits.Add(new LogHit(file, lineNo, "낮음", "읽기 경고", ex.Message, null));
            }
            catch (UnauthorizedAccessException ex)
            {
                summary.Hits.Add(new LogHit(file, lineNo, "낮음", "접근 경고", ex.Message, null));
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
