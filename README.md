# Runbook Lens

Local-first Windows desktop workspace for scanning logs, surfacing high-risk signals, and exporting incident briefs.

## Positioning

Runbook Lens is for SMB operators, support teams, solo developers, and local agent operators who need to triage log folders without uploading customer or system data to a SaaS tool.

It is not a generic TODO app. It is a workflow tool:

1. Pick a folder or single log file.
2. Select risk signal rules such as exceptions, timeouts, auth failures, data-loss risk, payment/order risk, and generic errors.
3. Scan locally.
4. Filter and select evidence rows.
5. Add operator notes and checklist context.
6. Export a Markdown incident brief or JSON session.

## MVP features

- C# / .NET 8 Windows Forms project.
- `net8.0-windows`, `UseWindowsForms=true`, `EnableWindowsTargeting=true` configured.
- Local file/folder scanner for `.log`, `.txt`, `.json`, `.csv`, `.md`, `.out`, `.trace`.
- Built-in regex rule pack for operational incidents.
- Results grid with severity, rule, file path, line number, preview, and timestamp extraction.
- Text filter across file/rule/severity/preview.
- Triage checklist and operator notes panel.
- Markdown brief export and JSON session export.
- No external paid API, login, outbound messaging, or credential storage.

## Build and run on Windows

Prerequisite: .NET 8 SDK on Windows.

```powershell
cd C:\path\to\runbook-lens
dotnet restore .\RunbookLens.sln
dotnet build .\RunbookLens.sln -c Release
dotnet run --project .\src\RunbookLens\RunbookLens.csproj
```

For a fuller Windows verification/publish pass:

```powershell
.\scripts\verify-windows.ps1
```

Manual smoke test:

1. Start the app.
2. Choose `samples\demo.txt` or a folder containing logs.
3. Click `Scan locally`.
4. Select rows and export Markdown or JSON.

## Current validation status

Windows Forms targets Windows, but this repository is configured with `EnableWindowsTargeting=true`, so non-Windows SDK restore/build/publish checks can catch most compile-time issues.

Validated from macOS with local .NET 8 SDK:

```bash
dotnet restore RunbookLens.sln
dotnet build RunbookLens.sln -c Release --no-restore
dotnet publish src/RunbookLens/RunbookLens.csproj -c Release -r win-x64 --self-contained false
```

Result: restore/build/publish completed with 0 warnings and 0 errors. Actual WinForms UI launch still requires Windows.

## File structure

```text
RunbookLens.sln
src/RunbookLens/RunbookLens.csproj
src/RunbookLens/Program.cs
src/RunbookLens/MainForm.cs
src/RunbookLens/Models/DomainModels.cs
src/RunbookLens/Services/LogScanner.cs
src/RunbookLens/Services/ReportExporter.cs
README.md
docs/DESIGN.md
scripts/verify-windows.ps1
samples/demo.txt
```

## Safety/privacy

- Scans local files only.
- Does not transmit logs.
- Does not create, request, or store secrets.
- Export files are written only to the user-selected path.
