# Runbook Lens

로컬 로그를 빠르게 훑고, 위험 신호를 찾고, 인수인계용 점검 보고서를 만드는 Windows 데스크톱 앱입니다.

## 포지셔닝

Runbook Lens는 고객/시스템 로그를 SaaS에 업로드하지 않고 로컬에서만 확인해야 하는 운영자, 지원팀, 1인 개발자, 에이전트 운영자를 위한 도구입니다.

단순 TODO 앱이 아니라 다음 흐름을 돕는 운영 점검 워크스페이스입니다.

1. 로그 폴더 또는 단일 로그 파일 선택
2. 예외, 타임아웃, 인증 실패, 데이터 손실 위험, 결제/주문 위험 같은 점검 규칙 선택
3. 로컬 스캔 실행
4. 결과를 필터링하고 핵심 증거 행 선택
5. 운영 메모/원인 가설/다음 조치 작성
6. Markdown 점검 보고서 또는 JSON 세션 내보내기

## MVP 기능

- C# / .NET 8 Windows Forms 프로젝트
- `net8.0-windows`, `UseWindowsForms=true`, `EnableWindowsTargeting=true` 설정
- `.log`, `.txt`, `.json`, `.csv`, `.md`, `.out`, `.trace` 파일/폴더 로컬 스캔
- 운영 사고용 기본 regex 규칙팩
- 심각도, 규칙, 파일 경로, 라인 번호, 미리보기, 타임스탬프 추출 결과 그리드
- 텍스트/심각도/규칙/파일명 필터
- 점검 체크리스트와 운영 메모 패널
- Markdown 보고서 및 JSON 세션 내보내기
- 외부 유료 API, 로그인, 외부 발송, credential 저장 없음

## Windows에서 빌드/실행

필수 조건: Windows에 .NET 8 SDK 설치

```powershell
cd C:\path\to\runbook-lens
dotnet restore .\RunbookLens.sln
dotnet build .\RunbookLens.sln -c Release
dotnet run --project .\src\RunbookLens\RunbookLens.csproj
```

Visual Studio에서는 `RunbookLens.sln`을 열고 `F5` 또는 `Ctrl+F5`로 실행하면 됩니다.

전체 검증/게시 스크립트:

```powershell
.\scripts\verify-windows.ps1
```

수동 확인 절차:

1. 앱 실행
2. `samples\demo.txt` 또는 로그 폴더 선택
3. `로컬 스캔 시작` 클릭
4. 결과 행 선택 후 Markdown 또는 JSON으로 내보내기

## 현재 검증 상태

Windows Forms는 Windows UI 런타임이 필요합니다. 다만 이 repo는 `EnableWindowsTargeting=true`로 설정되어 있어 비-Windows 환경에서도 restore/build/publish 검증으로 대부분의 컴파일 문제를 잡을 수 있습니다.

macOS + 로컬 .NET 8 SDK에서 검증 완료:

```bash
dotnet restore RunbookLens.sln
dotnet build RunbookLens.sln -c Release --no-restore
dotnet publish src/RunbookLens/RunbookLens.csproj -c Release -r win-x64 --self-contained false
```

결과: restore/build/publish 경고 0개, 오류 0개. 실제 WinForms UI 실행은 Windows에서 확인해야 합니다.

## 파일 구조

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

## 안전/프라이버시

- 로컬 파일만 스캔합니다.
- 로그를 외부로 전송하지 않습니다.
- 시크릿을 생성/요청/저장하지 않습니다.
- 내보내기 파일은 사용자가 선택한 경로에만 저장됩니다.
