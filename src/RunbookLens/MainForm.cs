using RunbookLens.Models;
using RunbookLens.Services;

namespace RunbookLens;

public sealed class MainForm : Form
{
    private readonly LogScanner _scanner = new();
    private readonly ReportExporter _exporter = new();
    private readonly BindingSource _hitsBinding = new();
    private readonly CheckedListBox _rulesList = new();
    private readonly TextBox _pathBox = new();
    private readonly TextBox _notesBox = new();
    private readonly TextBox _searchBox = new();
    private readonly NumericUpDown _maxFiles = new();
    private readonly Label _status = new();
    private readonly DataGridView _grid = new();
    private readonly ListBox _checklist = new();
    private ScanSummary _lastSummary = new();
    private CancellationTokenSource? _scanCancellation;

    public MainForm()
    {
        Text = "Runbook Lens - 로컬 로그 점검 워크스페이스";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BuildUi();
        LoadRules();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(10) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 9, ColumnCount = 1 };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.Controls.Add(left, 0, 0);

        var browse = new Button { Text = "로그 폴더/파일 선택", Dock = DockStyle.Fill };
        browse.Click += (_, _) => ChoosePath();
        left.Controls.Add(browse, 0, 0);
        _pathBox.PlaceholderText = "C:\\logs 또는 단일 로그 파일";
        _pathBox.Dock = DockStyle.Fill;
        left.Controls.Add(_pathBox, 0, 1);

        _rulesList.Dock = DockStyle.Fill;
        left.Controls.Add(Group("점검 규칙", _rulesList), 0, 2);

        var scan = new Button { Text = "로컬 스캔 시작", Dock = DockStyle.Fill };
        scan.Click += async (_, _) => await ScanAsync();
        left.Controls.Add(scan, 0, 3);

        _maxFiles.Minimum = 1;
        _maxFiles.Maximum = 5000;
        _maxFiles.Value = 500;
        _maxFiles.Dock = DockStyle.Fill;
        left.Controls.Add(Group("최대 파일 수", _maxFiles), 0, 4);

        _checklist.Items.AddRange(new object[]
        {
            "1. 심각/높음 신호 먼저 확인",
            "2. 영향 파일/시간대 확인",
            "3. 핵심 증거를 보고서에 반영",
            "4. 원인 가설/조치 메모 작성",
            "5. 인수인계용 Markdown 내보내기"
        });
        _checklist.Dock = DockStyle.Fill;
        left.Controls.Add(Group("점검 체크리스트", _checklist), 0, 5);

        _notesBox.Multiline = true;
        _notesBox.ScrollBars = ScrollBars.Vertical;
        _notesBox.Dock = DockStyle.Fill;
        _notesBox.PlaceholderText = "운영 메모, 원인 가설, 다음 조치...";
        left.Controls.Add(Group("사고/이슈 메모", _notesBox), 0, 6);

        var export = new Button { Text = "점검 보고서 내보내기", Dock = DockStyle.Fill };
        export.Click += (_, _) => ExportBrief();
        left.Controls.Add(export, 0, 7);
        _status.Text = "준비됨. 네트워크 전송 없음 · 시크릿 저장 없음 · 로컬 파일만 사용.";
        _status.Dock = DockStyle.Fill;
        left.Controls.Add(_status, 0, 8);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.Controls.Add(right, 1, 0);

        _searchBox.PlaceholderText = "텍스트, 심각도, 규칙, 파일명으로 결과 필터...";
        _searchBox.Dock = DockStyle.Fill;
        _searchBox.TextChanged += (_, _) => ApplyFilter();
        right.Controls.Add(_searchBox, 0, 0);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = true;
        _grid.DataSource = _hitsBinding;
        right.Controls.Add(_grid, 0, 1);

        var help = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Text = "사용 흐름: 로그 폴더/파일 선택 → 점검 규칙 선택 → 스캔 → 위험 신호 필터링 → 메모 작성 → Markdown/JSON 보고서 내보내기. " +
                   "SaaS 업로드 없이 로컬에서만 운영 증거를 확인해야 하는 운영자, 지원팀, 개발자를 위한 도구입니다."
        };
        right.Controls.Add(help, 0, 2);
    }

    private static GroupBox Group(string title, Control child)
    {
        var box = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(8) };
        child.Dock = DockStyle.Fill;
        box.Controls.Add(child);
        return box;
    }

    private void LoadRules()
    {
        foreach (var rule in _scanner.DefaultRules) _rulesList.Items.Add(rule, true);
    }

    private void ChoosePath()
    {
        var choice = MessageBox.Show(
            this,
            "폴더를 선택하려면 [예], 단일 로그 파일을 선택하려면 [아니요], 현재 경로를 유지하려면 [취소]를 누르세요.",
            "Runbook Lens",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (choice == DialogResult.Yes)
        {
            using var folderDialog = new FolderBrowserDialog { Description = "로그가 들어있는 폴더 선택" };
            if (folderDialog.ShowDialog(this) == DialogResult.OK) _pathBox.Text = folderDialog.SelectedPath;
            return;
        }

        if (choice == DialogResult.No)
        {
            using var fileDialog = new OpenFileDialog
            {
                Title = "로그/텍스트 파일 선택",
                Filter = "로그/텍스트 파일|*.log;*.txt;*.json;*.csv;*.md;*.out;*.trace|모든 파일|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (fileDialog.ShowDialog(this) == DialogResult.OK) _pathBox.Text = fileDialog.FileName;
        }
    }

    private async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(_pathBox.Text) || (!Directory.Exists(_pathBox.Text) && !File.Exists(_pathBox.Text)))
        {
            MessageBox.Show(this, "먼저 존재하는 폴더 또는 파일을 선택하세요.", "Runbook Lens", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var rules = _rulesList.CheckedItems.Cast<SignalRule>().ToList();
        if (rules.Count == 0)
        {
            MessageBox.Show(this, "점검 규칙을 하나 이상 선택하세요.", "Runbook Lens", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            _scanCancellation?.Cancel();
            _scanCancellation = new CancellationTokenSource();
            _status.Text = "스캔 중...";
            Cursor = Cursors.WaitCursor;
            _lastSummary = await _scanner.ScanAsync(_pathBox.Text, rules, (int)_maxFiles.Value, _scanCancellation.Token);
            ApplyFilter();
            _status.Text = $"완료: {_lastSummary.FilesScanned}개 파일, {_lastSummary.LinesScanned}줄, {_lastSummary.Hits.Count}개 결과.";
        }
        catch (OperationCanceledException)
        {
            _status.Text = "스캔이 취소되었습니다.";
        }
        catch (Exception ex)
        {
            _status.Text = "스캔 실패.";
            MessageBox.Show(this, ex.Message, "Runbook Lens 스캔 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void ApplyFilter()
    {
        var filter = _searchBox.Text.Trim();
        IEnumerable<LogHit> hits = _lastSummary.Hits;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            hits = hits.Where(hit =>
                hit.FilePath.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                hit.RuleName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                hit.Severity.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                hit.Preview.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }
        _hitsBinding.DataSource = hits.ToList();
    }

    private void ExportBrief()
    {
        if (_lastSummary.Hits.Count == 0)
        {
            MessageBox.Show(this, "먼저 스캔하세요. 내보낼 결과가 없습니다.", "Runbook Lens", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new SaveFileDialog
        {
            Filter = "Markdown 보고서 (*.md)|*.md|JSON 세션 (*.json)|*.json",
            FileName = $"runbook-lens-brief-{DateTime.Now:yyyyMMdd-HHmm}.md"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var selected = _grid.SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<LogHit>()
            .ToList();
        if (selected.Count == 0) selected = _lastSummary.Hits.Take(50).ToList();
        try
        {
            var output = dialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? _exporter.SaveSession(dialog.FileName, _lastSummary)
                : _exporter.ExportMarkdown(dialog.FileName, _lastSummary, selected, _notesBox.Text);
            _status.Text = "내보내기 완료: " + output;
        }
        catch (Exception ex)
        {
            _status.Text = "내보내기 실패.";
            MessageBox.Show(this, ex.Message, "Runbook Lens 내보내기 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
