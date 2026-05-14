using RunbookLens.Models;
using RunbookLens.Services;
using Xunit;

namespace RunbookLens.Tests;

public sealed class LogScannerTests
{
    [Fact]
    public async Task ScanAsync_AppliesMaxFileBytesAndRecordsSkippedFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            var small = Path.Combine(root, "small.log");
            var large = Path.Combine(root, "large.log");
            await File.WriteAllTextAsync(small, "2026-05-10 ERROR small failure\n");
            await File.WriteAllTextAsync(large, new string('x', 2_000) + " ERROR hidden\n");

            var scanner = new LogScanner();
            var summary = await scanner.ScanAsync(
                root,
                scanner.DefaultRules,
                new ScanOptions(MaxFiles: 10, MaxFileBytes: 512),
                CancellationToken.None);

            Assert.Equal(1, summary.FilesScanned);
            Assert.Equal(1, summary.FilesSkipped);
            Assert.Contains(summary.SkippedFiles, skipped => skipped.FilePath == large && skipped.Reason.Contains("크기"));
            Assert.Contains(summary.Hits, hit => hit.FilePath == small && hit.RuleName == "일반 오류");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_ProducesSeverityCountsInRiskOrder()
    {
        var root = CreateTempDirectory();
        try
        {
            var log = Path.Combine(root, "app.log");
            await File.WriteAllTextAsync(log, string.Join(Environment.NewLine,
                "2026-05-10 ERROR disk full",
                "2026-05-10 timeout while calling provider",
                "2026-05-10 error generic failure"));

            var scanner = new LogScanner();
            var summary = await scanner.ScanAsync(
                log,
                scanner.DefaultRules,
                new ScanOptions(MaxFiles: 10, MaxFileBytes: 1024 * 1024),
                CancellationToken.None);

            var counts = summary.GetSeverityCounts().ToList();

            Assert.Equal("심각", counts[0].Severity);
            Assert.Equal("높음", counts[1].Severity);
            Assert.Contains(counts, item => item.Severity == "중간" && item.Count >= 1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_SampleFolderContainsBroadDemoSignals()
    {
        var samples = FindRepositoryPath("samples");
        var scanner = new LogScanner();

        var summary = await scanner.ScanAsync(
            samples,
            scanner.DefaultRules,
            new ScanOptions(MaxFiles: 20, MaxFileBytes: 1024 * 1024),
            CancellationToken.None);

        Assert.True(summary.FilesScanned >= 5);
        Assert.Contains(summary.Hits, hit => hit.RuleName == "처리되지 않은 예외");
        Assert.Contains(summary.Hits, hit => hit.RuleName == "타임아웃/지연");
        Assert.Contains(summary.Hits, hit => hit.RuleName == "인증/권한 문제");
        Assert.Contains(summary.Hits, hit => hit.RuleName == "데이터 손실 위험");
        Assert.Contains(summary.Hits, hit => hit.RuleName == "결제/주문 위험");
        Assert.Contains(summary.Hits, hit => hit.RuleName == "일반 오류");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "runbook-lens-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FindRepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (Directory.Exists(candidate) || File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find repository path: {relativePath}");
    }
}
