# Runbook Lens

Runbook Lens는 로컬 로그를 스캔해 위험 신호를 빠르게 찾고, 운영 인수인계용 점검 보고서를 만드는 Windows 데스크톱 앱입니다.

## 제품 한 줄 설명

Windows 환경의 로그 파일/폴더를 외부 전송 없이 로컬에서 분석하고, 예외·타임아웃·인증 실패·데이터 손실·결제/주문 위험 같은 운영 신호를 보고서로 정리하는 로컬 퍼스트 점검 도구입니다.

## 주요 기능

- Windows Forms 기반 데스크톱 UI (`.NET 8`, `net8.0-windows`)
- 단일 로그 파일 또는 로그 폴더 선택
- `.log`, `.txt`, `.json`, `.csv`, `.md`, `.out`, `.trace` 파일 스캔
- 기본 운영 사고 규칙팩 제공
  - 처리되지 않은 예외
  - 타임아웃/지연
  - 인증/권한 문제
  - 데이터 손실 위험
  - 결제/주문 위험
  - 일반 오류
- 결과 그리드 제공
  - 심각도
  - 규칙명
  - 파일 경로
  - 라인 번호
  - 미리보기
  - 타임스탬프 추출값
- 텍스트/심각도/규칙/파일명 기준 결과 필터
- 운영 메모 및 점검 체크리스트 작성
- Markdown 점검 보고서 내보내기
- JSON 세션 내보내기
- 파일당 최대 크기 제한으로 대용량 로그 스캔 사고 방지
- 제외된 파일 목록/사유 기록
- 심각도별 결과 집계

## 최근 고도화 기능

- 스캔/모델/보고서 로직을 `RunbookLens.Core`로 분리
- xUnit 기반 테스트 프로젝트 추가
- 파일당 최대 크기 제한 옵션 추가
- 크기 제한 등으로 제외된 파일과 제외 사유 기록
- 스캔 완료 상태와 Markdown 보고서에 제외 파일 수 표시
- 심각도별 집계 기능 추가
- Windows UI 프로젝트와 Core 로직을 분리해 비-Windows 환경에서도 Core 테스트 가능

## 화면/사용 흐름

앱의 기본 흐름은 다음과 같습니다.

1. `로그 폴더/파일 선택` 클릭
2. 폴더 또는 단일 로그 파일 선택
3. 필요한 점검 규칙 선택
4. `최대 파일 수`, `파일당 최대 크기(MB)` 확인
5. `로컬 스캔 시작` 클릭
6. 결과 그리드에서 위험 신호 확인
7. 검색창으로 텍스트/심각도/규칙/파일명 필터링
8. 핵심 결과 행 선택
9. 운영 메모/원인 가설/다음 조치 작성
10. `점검 보고서 내보내기`로 Markdown 또는 JSON 저장

선택한 결과 행이 없으면 Markdown 보고서에는 기본적으로 상위 결과 일부가 포함됩니다.

## Visual Studio 실행 방법

필수 조건:

- Windows
- Visual Studio 2022 권장
- .NET 8 SDK
- `.NET desktop development` 워크로드

실행:

1. Visual Studio에서 `RunbookLens.sln` 열기
2. 시작 프로젝트가 `RunbookLens`인지 확인
3. `F5` 또는 `Ctrl+F5`로 실행
4. `samples/demo.txt` 또는 테스트할 로그 폴더 선택
5. 스캔 후 결과/필터/내보내기 동작 확인

주의: 실제 Windows Forms UI 런타임 동작은 Windows 환경에서 확인해야 합니다. 이 Mac 환경에서는 UI 실행 자체를 직접 검증할 수 없습니다.

## PowerShell 검증 방법

Windows PowerShell 또는 PowerShell 7에서 실행:

```powershell
cd C:\path\to\runbook-lens

dotnet restore .\RunbookLens.sln
dotnet build .\RunbookLens.sln -c Release --no-restore
dotnet test .\tests\RunbookLens.Tests\RunbookLens.Tests.csproj -c Release --no-restore
```

전체 검증 및 publish까지 한 번에 실행하려면:

```powershell
.\scripts\verify-windows.ps1
```

스크립트는 다음 작업을 수행합니다.

1. `dotnet --info` 출력
2. solution restore
3. Release build
4. xUnit 테스트 실행
5. `win-x64` framework-dependent publish
6. publish 결과물 `RunbookLens.exe` 존재 여부 확인

## publish 방법

수동 publish 예시:

```powershell
dotnet publish .\src\RunbookLens\RunbookLens.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=false `
  -o .\artifacts\publish\win-x64-framework-dependent
```

publish 결과물:

```text
artifacts/publish/win-x64-framework-dependent/RunbookLens.exe
```

## 샘플 로그 테스트 방법

샘플 폴더: `samples/`

포함된 샘플:

- `demo.txt`: 단일 파일 빠른 스모크 테스트
- `checkout-incident.log`: 결제/주문 장애와 worker exception 흐름
- `auth-access.log`: 인증/권한 실패와 로컬 저장 실패 흐름
- `sync-worker.json`: JSON lines 형태의 batch sync/data corruption 흐름
- `support-desk-export.md`: 지원 상담 export 안에 섞인 주문/환불 위험 신호

샘플에는 다음 유형의 신호가 포함되어 있습니다.

- 디스크 공간/쓰기 실패/out of space
- 결제 실패, 주문 실패, 환불, chargeback
- `NullReferenceException`, `InvalidOperationException`, stack trace
- 인증 실패/401/403/permission denied/invalid token
- timeout/deadline exceeded/slow query/latency
- corrupt export/data safety risk

UI에서 테스트:

1. 앱 실행
2. `로그 폴더/파일 선택` 클릭
3. 폴더 선택으로 `samples/` 선택하거나 단일 파일 선택으로 `samples/demo.txt` 선택
4. 기본 규칙을 유지한 채 `로컬 스캔 시작` 클릭
5. 결과 그리드에 심각/높음/중간 신호가 표시되는지 확인
6. 검색창에 `payment`, `401`, `timeout`, `chargeback`, `심각` 등을 입력해 필터 확인
7. Markdown 또는 JSON으로 내보내기 확인

Core 테스트로 검증:

```powershell
dotnet test .\tests\RunbookLens.Tests\RunbookLens.Tests.csproj -c Release --no-restore
```

## 프로젝트 구조

```text
RunbookLens.sln
README.md
docs/
  DESIGN.md
samples/
  demo.txt
  checkout-incident.log
  auth-access.log
  sync-worker.json
  support-desk-export.md
scripts/
  verify-windows.ps1
src/
  RunbookLens/
    RunbookLens.csproj
    Program.cs
    MainForm.cs
  RunbookLens.Core/
    RunbookLens.Core.csproj
    Models/
      DomainModels.cs
    Services/
      LogScanner.cs
      ReportExporter.cs
tests/
  RunbookLens.Tests/
    RunbookLens.Tests.csproj
    LogScannerTests.cs
```

구성 요약:

- `src/RunbookLens`: Windows Forms UI 및 사용자 흐름
- `src/RunbookLens.Core`: 로그 스캔, 도메인 모델, 보고서 내보내기 로직
- `tests/RunbookLens.Tests`: Core 로직 xUnit 테스트
- `samples/`: 수동 테스트용 샘플 로그 묶음 (`demo.txt`, 결제/권한/sync/support export 샘플 포함)
- `scripts/verify-windows.ps1`: Windows 검증/publish 스크립트
- `docs/DESIGN.md`: 제품/아키텍처 설계 메모

## 로컬 퍼스트/보안 원칙

- 로그 파일은 로컬에서만 읽습니다.
- 로그 내용을 외부 서버/SaaS/API로 전송하지 않습니다.
- 외부 유료 API를 사용하지 않습니다.
- 로그인 기능이 없습니다.
- credential, token, password 같은 시크릿 값을 생성/요청/저장하지 않습니다.
- 내보내기 파일은 사용자가 선택한 로컬 경로에만 저장됩니다.
- 네트워크 전송 기능은 현재 포함되어 있지 않습니다.

## 현재 제한사항

- 실제 Windows Forms UI 런타임 검증은 사용자 Windows 환경에서 확인이 필요합니다.
- 이 Mac 환경에서는 `EnableWindowsTargeting=true` 설정을 통해 restore/build/test/publish 수준의 검증만 가능합니다.
- 기본 규칙팩은 고정되어 있으며, 사용자 정의 규칙 관리 UI는 아직 없습니다.
- 타임라인 차트, 저장된 워크스페이스, 두 스캔 간 diff 기능은 아직 없습니다.
- 설치 프로그램/코드 서명은 아직 없습니다.
- regex 기반 탐지이므로 false positive/false negative가 발생할 수 있습니다.

## 현재 검증 상태

이 저장소는 비-Windows 환경에서도 컴파일 검증이 가능하도록 `EnableWindowsTargeting=true`를 사용합니다.

권장 검증 명령:

```bash
dotnet restore RunbookLens.sln
dotnet build RunbookLens.sln -c Release --no-restore
dotnet test tests/RunbookLens.Tests/RunbookLens.Tests.csproj -c Release --no-restore
dotnet publish src/RunbookLens/RunbookLens.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o artifacts/publish/win-x64-framework-dependent
```

단, 위 명령은 빌드/테스트/publish 검증이며 실제 Windows UI 실행 검증을 대체하지 않습니다.
