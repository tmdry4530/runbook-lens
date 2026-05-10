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
        Text = "Runbook Lens - local log triage workspace";
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

        var browse = new Button { Text = "Choose log folder/file", Dock = DockStyle.Fill };
        browse.Click += (_, _) => ChoosePath();
        left.Controls.Add(browse, 0, 0);
        _pathBox.PlaceholderText = "C:\\logs or single .log file";
        _pathBox.Dock = DockStyle.Fill;
        left.Controls.Add(_pathBox, 0, 1);

        _rulesList.Dock = DockStyle.Fill;
        left.Controls.Add(Group("Signal rules", _rulesList), 0, 2);

        var scan = new Button { Text = "Scan locally", Dock = DockStyle.Fill };
        scan.Click += async (_, _) => await ScanAsync();
        left.Controls.Add(scan, 0, 3);

        _maxFiles.Minimum = 1;
        _maxFiles.Maximum = 5000;
        _maxFiles.Value = 500;
        _maxFiles.Dock = DockStyle.Fill;
        left.Controls.Add(Group("Max files", _maxFiles), 0, 4);

        _checklist.Items.AddRange(new object[]
        {
            "1. Identify first critical/high signal",
            "2. Confirm impacted file/time window",
            "3. Capture evidence into brief",
            "4. Add operator hypothesis",
            "5. Export markdown for handoff"
        });
        _checklist.Dock = DockStyle.Fill;
        left.Controls.Add(Group("Triage checklist", _checklist), 0, 5);

        _notesBox.Multiline = true;
        _notesBox.ScrollBars = ScrollBars.Vertical;
        _notesBox.Dock = DockStyle.Fill;
        _notesBox.PlaceholderText = "Operator notes, hypothesis, next actions...";
        left.Controls.Add(Group("Incident notes", _notesBox), 0, 6);

        var export = new Button { Text = "Export incident brief", Dock = DockStyle.Fill };
        export.Click += (_, _) => ExportBrief();
        left.Controls.Add(export, 0, 7);
        _status.Text = "Ready. No network, no secrets, local files only.";
        _status.Dock = DockStyle.Fill;
        left.Controls.Add(_status, 0, 8);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.Controls.Add(right, 1, 0);

        _searchBox.PlaceholderText = "Filter hits by text, severity, rule, file...";
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
            Text = "MVP workflow: pick a log folder → choose risk rules → scan → filter high-signal rows → add notes → export a Markdown incident brief. " +
                   "Designed for SMB operators, support desks, and solo developers who need a local-first triage tool without SaaS upload risk."
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
            "Choose Yes for a folder, No for a single log file, or Cancel to keep the current path.",
            "Runbook Lens",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (choice == DialogResult.Yes)
        {
            using var folderDialog = new FolderBrowserDialog { Description = "Choose folder containing logs" };
            if (folderDialog.ShowDialog(this) == DialogResult.OK) _pathBox.Text = folderDialog.SelectedPath;
            return;
        }

        if (choice == DialogResult.No)
        {
            using var fileDialog = new OpenFileDialog
            {
                Title = "Choose a log or text file",
                Filter = "Log and text files|*.log;*.txt;*.json;*.csv;*.md;*.out;*.trace|All files|*.*",
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
            MessageBox.Show(this, "Choose an existing folder or file first.", "Runbook Lens", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var rules = _rulesList.CheckedItems.Cast<SignalRule>().ToList();
        if (rules.Count == 0)
        {
            MessageBox.Show(this, "Select at least one signal rule.", "Runbook Lens", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            _scanCancellation?.Cancel();
            _scanCancellation = new CancellationTokenSource();
            _status.Text = "Scanning...";
            Cursor = Cursors.WaitCursor;
            _lastSummary = await _scanner.ScanAsync(_pathBox.Text, rules, (int)_maxFiles.Value, _scanCancellation.Token);
            ApplyFilter();
            _status.Text = $"Done: {_lastSummary.FilesScanned} files, {_lastSummary.LinesScanned} lines, {_lastSummary.Hits.Count} hits.";
        }
        catch (OperationCanceledException)
        {
            _status.Text = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            _status.Text = "Scan failed.";
            MessageBox.Show(this, ex.Message, "Runbook Lens scan error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(this, "Scan first; there are no hits to export.", "Runbook Lens", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new SaveFileDialog
        {
            Filter = "Markdown brief (*.md)|*.md|JSON session (*.json)|*.json",
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
            _status.Text = "Exported: " + output;
        }
        catch (Exception ex)
        {
            _status.Text = "Export failed.";
            MessageBox.Show(this, ex.Message, "Runbook Lens export error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
