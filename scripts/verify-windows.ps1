$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'RunbookLens.sln'
$project = Join-Path $repoRoot 'src/RunbookLens/RunbookLens.csproj'
$publishDir = Join-Path $repoRoot 'artifacts/publish/win-x64-framework-dependent'

Write-Host '== Runbook Lens Windows 검증 =='
dotnet --info

dotnet restore $solution
dotnet build $solution -c Release --no-restore
dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o $publishDir

$exe = Join-Path $publishDir 'RunbookLens.exe'
if (!(Test-Path $exe)) {
  throw "게시 결과물이 생성되지 않음: $exe"
}

Write-Host "OK: 빌드/게시 완료"
Write-Host "수동 UI 확인: $exe 실행 → samples/demo.txt 또는 로그 폴더 선택 → 스캔 → Markdown/JSON 내보내기 확인."
