# Runbook Lens

> 로컬 로그를 빠르게 스캔해서 운영 위험 신호를 찾고, 인수인계용 점검 보고서까지 만드는 Windows 데스크톱 앱

Runbook Lens는 Windows 환경에 흩어진 로그 파일, 주문/결제 export, 지원 상담 기록, agent 실행 결과를 외부 서버로 보내지 않고 로컬에서 분석하는 운영 점검 도구입니다. 발표에서는 **“로그 폴더 선택 → 로컬 스캔 → 위험 신호 확인 → 메모 작성 → Markdown/JSON 보고서 내보내기”** 흐름을 보여주면 됩니다.

---

## 1. 발표용 한 줄 요약

**Runbook Lens는 장애 대응자가 로그를 하나씩 열어보는 시간을 줄이고, 예외·타임아웃·인증 실패·데이터 손실·결제/주문 위험을 자동으로 찾아 운영 보고서로 정리해주는 로컬 퍼스트 로그 점검 앱입니다.**

---

## 2. 왜 만들었나

운영 장애나 고객 이슈가 생기면 보통 이런 일이 반복됩니다.

1. 여러 폴더에 흩어진 로그/텍스트/export 파일을 직접 연다.
2. `error`, `timeout`, `401`, `payment failed` 같은 키워드를 수동 검색한다.
3. 중요한 줄을 복사해서 메모장/노션/슬랙에 붙인다.
4. 어떤 파일에서, 몇 번째 줄에서, 어떤 위험인지 다시 정리한다.
5. 다음 담당자에게 넘길 보고서를 따로 작성한다.

Runbook Lens는 이 반복 작업을 한 화면에서 처리합니다.

- 로그는 로컬에서만 읽음
- 외부 API/SaaS 업로드 없음
- 운영 위험 규칙팩으로 자동 탐지
- 결과를 필터링하고 메모 추가
- Markdown/JSON 보고서로 바로 export

---

## 3. 핵심 기능

| 기능 | 설명 |
|---|---|
| 로컬 파일/폴더 선택 | 단일 로그 파일 또는 로그 폴더 전체 선택 |
| 다중 확장자 스캔 | `.log`, `.txt`, `.json`, `.csv`, `.md`, `.out`, `.trace` 지원 |
| 기본 규칙팩 | 예외, 타임아웃, 인증/권한, 데이터 손실, 결제/주문, 일반 오류 탐지 |
| 결과 그리드 | 심각도, 규칙명, 파일 경로, 라인 번호, 미리보기, 타임스탬프 표시 |
| 결과 필터 | 텍스트, 심각도, 규칙명, 파일명 기준 검색 |
| 대용량 보호 | 최대 파일 수, 파일당 최대 크기 제한 |
| 제외 파일 기록 | 크기 제한 등으로 스캔하지 않은 파일과 사유 기록 |
| 운영 메모 | 원인 가설, 영향 범위, 다음 조치 메모 작성 |
| 보고서 내보내기 | Markdown 점검 보고서 또는 JSON 세션 저장 |
| Core 테스트 | UI와 분리된 스캔/보고서 로직 xUnit 테스트 |

---

## 4. 작동 흐름

```text
[사용자]
  ↓
로그 폴더/파일 선택
  ↓
점검 규칙 선택
  ↓
최대 파일 수 / 파일당 최대 크기 확인
  ↓
로컬 스캔 시작
  ↓
파일 열기 → 한 줄씩 읽기 → 규칙 regex 매칭 → LogHit 생성
  ↓
결과 그리드 표시
  ↓
검색/심각도/규칙/파일명으로 필터링
  ↓
핵심 결과 선택 + 운영 메모 작성
  ↓
Markdown 보고서 또는 JSON 세션 내보내기
```

발표 데모 순서도 이 흐름 그대로 가면 됩니다.

---

## 5. 구현 흐름

프로젝트는 UI와 핵심 로직을 분리했습니다.

```text
RunbookLens.sln
├─ src/RunbookLens              # Windows Forms UI
│  ├─ Program.cs
│  └─ MainForm.cs               # 화면 구성, 사용자 이벤트, 스캔/내보내기 orchestration
├─ src/RunbookLens.Core         # OS 독립적인 핵심 로직
│  ├─ Models/DomainModels.cs    # SignalRule, LogHit, ScanSummary, ScanOptions
│  └─ Services
│     ├─ LogScanner.cs          # 파일 열거, 라인 스캔, 규칙 매칭, timestamp 추출
│     └─ ReportExporter.cs      # Markdown/JSON export
├─ tests/RunbookLens.Tests      # xUnit 테스트
├─ samples/                     # 발표/QA용 샘플 로그
├─ scripts/verify-windows.ps1   # Windows 검증 + publish 스크립트
└─ docs/DESIGN.md               # 제품/아키텍처 설계 메모
```

### 5.1 UI 계층: `src/RunbookLens`

`MainForm.cs`가 사용자의 전체 작업 흐름을 담당합니다.

1. WinForms 화면 생성
   - 왼쪽: 파일 선택, 규칙 선택, 스캔 옵션, 체크리스트, 메모, 내보내기 버튼
   - 오른쪽: 검색창, 결과 그리드, 사용 흐름 안내
2. 기본 규칙팩을 체크리스트에 로드
3. 파일/폴더 선택 다이얼로그 실행
4. `LogScanner.ScanAsync()` 호출
5. 결과를 `DataGridView`에 바인딩
6. 검색창 입력마다 결과 필터링
7. 선택 결과와 메모를 `ReportExporter`로 전달

### 5.2 Core 계층: `src/RunbookLens.Core`

Core는 Windows Forms에 의존하지 않습니다. 그래서 Mac/Linux에서도 핵심 로직 테스트가 가능합니다.

- `SignalRule`: 탐지 규칙 이름, regex 패턴, 심각도, 설명
- `LogHit`: 탐지된 결과 1건. 파일 경로, 라인 번호, 심각도, 규칙명, 미리보기, timestamp 포함
- `ScanOptions`: 최대 파일 수, 파일당 최대 크기 제한
- `SkippedFile`: 스캔 제외 파일과 사유
- `ScanSummary`: 전체 스캔 결과, hit 목록, 제외 파일, 심각도 집계

### 5.3 스캔 로직: `LogScanner`

`LogScanner`는 다음 순서로 동작합니다.

1. 입력 경로가 파일인지 폴더인지 확인
2. 폴더라면 지원 확장자 파일만 재귀적으로 열거
3. 최근 수정 파일 우선으로 정렬
4. 최대 파일 수만큼 후보 제한
5. 파일당 크기 제한 초과 시 `SkippedFile`로 기록
6. 파일을 `FileShare.ReadWrite`로 열어 실행 중인 로그도 최대한 읽기
7. 한 줄씩 읽으면서 모든 규칙 regex 적용
8. 매칭되면 `LogHit` 생성
9. `IOException`, `UnauthorizedAccessException`은 앱 crash 대신 낮은 심각도 경고로 기록
10. 스캔 시간, 파일 수, 라인 수, hit 수를 `ScanSummary`에 저장

기본 규칙팩:

| 규칙 | 심각도 | 탐지 예시 |
|---|---:|---|
| 처리되지 않은 예외 | 심각 | `exception`, `stack trace`, `NullReferenceException` |
| 데이터 손실 위험 | 심각 | `corrupt`, `failed to write`, `disk full`, `out of space` |
| 타임아웃/지연 | 높음 | `timeout`, `slow query`, `deadline exceeded` |
| 인증/권한 문제 | 높음 | `401`, `403`, `unauthorized`, `invalid token` |
| 결제/주문 위험 | 높음 | `payment failed`, `refund`, `chargeback`, `order failed` |
| 일반 오류 | 중간 | `error`, `failed`, `failure`, `fatal` |

### 5.4 내보내기 로직: `ReportExporter`

보고서는 두 가지 형식으로 저장됩니다.

- Markdown: 운영 인수인계/발표/리뷰용 사람이 읽는 보고서
- JSON: 스캔 세션을 다른 도구가 다시 처리할 수 있는 구조화 데이터

Markdown 보고서에는 다음 내용이 들어갑니다.

1. 생성 시각
2. 스캔 파일 수
3. 제외 파일 수
4. 스캔 라인 수
5. 선택 결과 수
6. 운영 메모
7. 심각도별 집계
8. 제외된 파일 목록
9. 선택한 증거 목록

---

## 6. 데이터 흐름

```text
파일/폴더 경로
  ↓
EnumerateCandidateFiles()
  ↓
지원 확장자 필터 + 최근 수정순 정렬
  ↓
ScanOptions 적용
  ├─ MaxFiles
  └─ MaxFileBytes
  ↓
라인 단위 읽기
  ↓
SignalRule regex 매칭
  ↓
LogHit 목록 생성
  ↓
ScanSummary 생성
  ↓
UI 결과 그리드 + 심각도 집계
  ↓
ReportExporter
  ├─ Markdown 보고서
  └─ JSON 세션
```

---

## 7. 발표 데모 시나리오

### 데모 목표

“로그를 직접 뒤지는 대신, Runbook Lens가 위험 신호를 자동으로 모아서 보고서까지 만든다”를 보여줍니다.

### 준비물

- Windows PC
- Visual Studio 2022 또는 .NET 8 SDK
- 이 저장소 clone
- `samples/` 폴더

### 추천 데모 순서

1. 앱 실행
2. `로그 폴더/파일 선택` 클릭
3. `samples/` 폴더 선택
4. 기본 규칙 전체 선택 상태 유지
5. `최대 파일 수 = 500`, `파일당 최대 크기 = 25MB` 확인
6. `로컬 스캔 시작` 클릭
7. 상태바에서 스캔 파일 수/라인 수/결과 수 확인
8. 검색창에 아래 키워드를 하나씩 입력
   - `payment`
   - `401`
   - `timeout`
   - `chargeback`
   - `심각`
9. 결과 행 몇 개 선택
10. 운영 메모에 원인 가설과 다음 조치 작성
11. `점검 보고서 내보내기` 클릭
12. Markdown 파일 저장
13. 생성된 보고서를 열어 인수인계 자료 형태 확인

### 발표 멘트 예시

> “장애 대응자가 가장 먼저 하는 일은 로그에서 증거를 찾는 겁니다. Runbook Lens는 로그를 외부로 올리지 않고 로컬에서만 스캔합니다. 예외, 타임아웃, 인증 실패, 결제 실패 같은 운영 신호를 규칙 기반으로 잡아내고, 선택한 증거와 운영 메모를 바로 Markdown 보고서로 내보냅니다.”

---

## 8. 샘플 로그 구성

샘플 폴더: `samples/`

| 파일 | 용도 |
|---|---|
| `demo.txt` | 단일 파일 빠른 스모크 테스트 |
| `checkout-incident.log` | 결제/주문 장애와 worker exception 흐름 |
| `auth-access.log` | 인증/권한 실패와 로컬 저장 실패 흐름 |
| `sync-worker.json` | JSON lines 형태의 batch sync/data corruption 흐름 |
| `support-desk-export.md` | 지원 상담 export 안에 섞인 주문/환불 위험 신호 |

샘플에는 다음 신호가 들어 있습니다.

- 디스크 공간 부족 / 쓰기 실패 / `out of space`
- 결제 실패 / 주문 실패 / 환불 / chargeback
- `NullReferenceException`, `InvalidOperationException`, stack trace
- 인증 실패 / `401` / `403` / permission denied / invalid token
- timeout / deadline exceeded / slow query / latency
- corrupt export / data safety risk

---

## 9. 실행 방법

### Visual Studio 실행

필수 조건:

- Windows
- Visual Studio 2022 권장
- .NET 8 SDK
- `.NET desktop development` 워크로드

실행:

1. Visual Studio에서 `RunbookLens.sln` 열기
2. 시작 프로젝트가 `RunbookLens`인지 확인
3. `F5` 또는 `Ctrl+F5`로 실행
4. `samples/` 폴더 또는 `samples/demo.txt` 선택
5. 스캔 → 필터 → 메모 → 내보내기 확인

주의: 실제 Windows Forms UI 런타임 동작은 Windows 환경에서 확인해야 합니다. 이 Mac 환경에서는 UI 실행 자체를 직접 검증할 수 없습니다.

### PowerShell 검증

```powershell
cd C:\path\to\runbook-lens

dotnet restore .\RunbookLens.sln
dotnet build .\RunbookLens.sln -c Release --no-restore
dotnet test .\tests\RunbookLens.Tests\RunbookLens.Tests.csproj -c Release --no-restore
```

전체 검증 및 publish까지 한 번에 실행:

```powershell
.\scripts\verify-windows.ps1
```

스크립트 수행 작업:

1. `dotnet --info` 출력
2. solution restore
3. Release build
4. xUnit 테스트 실행
5. `win-x64` framework-dependent publish
6. publish 결과물 `RunbookLens.exe` 존재 여부 확인

---

## 10. Publish 방법

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

---

## 11. 로컬 퍼스트 / 보안 원칙

Runbook Lens는 발표에서 이 점을 강조하면 좋습니다.

- 로그 파일은 로컬에서만 읽습니다.
- 로그 내용을 외부 서버/SaaS/API로 전송하지 않습니다.
- 외부 유료 API를 사용하지 않습니다.
- 로그인 기능이 없습니다.
- credential, token, password 같은 시크릿 값을 생성/요청/저장하지 않습니다.
- 내보내기 파일은 사용자가 선택한 로컬 경로에만 저장됩니다.
- 네트워크 전송 기능은 현재 포함되어 있지 않습니다.

---

## 12. 테스트 상태

이 저장소는 Windows UI 프로젝트를 포함하지만, 비-Windows 환경에서도 Core 로직과 빌드 검증이 가능하도록 `EnableWindowsTargeting=true`를 사용합니다.

권장 검증 명령:

```bash
dotnet restore RunbookLens.sln
dotnet build RunbookLens.sln -c Release --no-restore
dotnet test tests/RunbookLens.Tests/RunbookLens.Tests.csproj -c Release --no-restore
dotnet publish src/RunbookLens/RunbookLens.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o artifacts/publish/win-x64-framework-dependent
```

단, 위 명령은 빌드/테스트/publish 검증이며 실제 Windows UI 실행 검증을 대체하지 않습니다.

---

## 13. 현재 제한사항

- 실제 Windows Forms UI 런타임 검증은 Windows 환경에서 추가 확인이 필요합니다.
- 기본 규칙팩은 고정되어 있으며, 사용자 정의 규칙 관리 UI는 아직 없습니다.
- 타임라인 차트, 저장된 워크스페이스, 두 스캔 간 diff 기능은 아직 없습니다.
- 설치 프로그램/코드 서명은 아직 없습니다.
- regex 기반 탐지이므로 false positive/false negative가 발생할 수 있습니다.

---

## 14. 다음 개선 후보

1. 사용자 정의 규칙 추가/저장
2. 심각도별 색상 강조 및 결과 그룹핑
3. 시간대별 타임라인 차트
4. 이전 스캔과 현재 스캔 diff
5. 보고서 템플릿 선택
6. Windows installer / code signing
7. 샘플 로그 기반 발표용 스크린샷 추가
