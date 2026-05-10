$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'RunbookLens.sln'
$project = Join-Path $repoRoot 'src/RunbookLens/RunbookLens.csproj'
$publishDir = Join-Path $repoRoot 'artifacts/publish/win-x64-framework-dependent'

Write-Host '== Runbook Lens Windows verification =='
dotnet --info

dotnet restore $solution
dotnet build $solution -c Release --no-restore
dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o $publishDir

$exe = Join-Path $publishDir 'RunbookLens.exe'
if (!(Test-Path $exe)) {
  throw "Publish did not produce $exe"
}

Write-Host "OK: build/publish complete"
Write-Host "Manual UI smoke: run $exe, choose samples/demo.txt or a log folder, scan, then export markdown/json."
