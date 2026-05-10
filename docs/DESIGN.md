# Runbook Lens Design

## Product thesis

Windows desktop users still keep operational evidence in local folders: IIS/app logs, exported CRM/order files, support transcripts, and agent run outputs. Existing tools are either broad editors/search tools or heavyweight observability stacks. Runbook Lens fills the gap for quick, private, repeatable triage.

## User

- Solo developer operating a Windows desktop/server.
- SMB support/operator handling local exports and app logs.
- Internal agent/operator reviewing local run artifacts before handoff.

## Core workflow

1. Choose a folder or single log file.
2. Select signal rules.
3. Scan with a maximum file cap.
4. Review hits in a grid.
5. Filter to the incident window or subsystem.
6. Add notes and checklist state.
7. Export a concise incident brief.

## Architecture

- `MainForm`: WinForms UI and workflow orchestration.
- `LogScanner`: local file enumeration, line scanning, regex rule matching, timestamp extraction.
- `ReportExporter`: Markdown and JSON output.
- `Models`: immutable hit/rule records and scan summary DTOs.

## MVP boundaries

Included:

- Local scanner and fixed rule pack.
- Evidence grid and free-text filtering.
- Markdown/JSON export.
- Basic file/folder chooser.

Deferred:

- Custom user-managed rules UI.
- Timeline chart.
- Saved workspaces.
- Diff between two scans.
- Installer/signing.
- Windows runtime smoke on a real Windows host.

## Risk handling

- Large folders: MVP includes max file cap; future work should add streaming progress and size limits.
- Locked files: scanner records low severity read/access warnings instead of crashing.
- Regex false positives: built-in rules are broad; v1 should support custom tuning and per-rule disable persistence.
