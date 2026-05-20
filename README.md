# Runbook Lens

Runbook Lens는 장애 대응자가 운영 로그를 빠르게 훑고, 위험 신호를 골라 인수인계용 incident brief로 정리하는 **로컬 우선 C#/.NET 8 WinForms 데스크톱 앱**입니다.

외부 서버로 로그를 업로드하지 않습니다. Windows PC에서 파일/폴더를 선택하면 예외, 타임아웃, 인증 실패, 데이터 손실, 결제/주문 오류 같은 운영 위험 신호를 찾아서 심각도·규칙명·파일·라인·미리보기로 보여줍니다.

## 이런 상황에 씁니다

- 장애 직후 로그 파일이 여러 개라 원인 후보를 빨리 좁혀야 할 때
- 고객 이슈 재현 로그를 개발자에게 넘기기 전에 핵심 라인만 추려야 할 때
- 야간/주말 온콜 인수인계용 Markdown brief가 필요할 때
- 민감한 운영 로그를 외부 SaaS에 올리기 어려울 때

## 핵심 기능

- `.log`, `.txt`, `.json`, `.csv` 등 텍스트 로그 파일/폴더 스캔
- `error`, `warn`, `exception`, `timeout`, `unauthorized`, `payment`, `order` 계열 위험 키워드 탐지
- 심각도별 결과 테이블과 선택 이벤트 상세 보기
- 스캔 요약, 스킵 파일, 노트 포함
- Markdown incident brief / JSON 세션 내보내기
- UI 프로젝트와 Core 라이브러리 분리
- xUnit 기반 핵심 스캐너 테스트

## Windows 빠른 시작

필수: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
git clone https://github.com/tmdry4530/runbook-lens.git
cd runbook-lens
dotnet restore .\RunbookLens.sln
dotnet run --project .\src\RunbookLens\RunbookLens.csproj
```

릴리즈 zip을 받은 경우:

```powershell
Expand-Archive .\RunbookLens-win-x64-v0.1.1.zip .\RunbookLens
.\RunbookLens\RunbookLens.exe
```

## 기본 사용 흐름

1. `Select File` 또는 `Select Folder`로 로그 입력을 고릅니다.
2. `Scan`을 눌러 위험 신호를 탐지합니다.
3. 심각도와 규칙명 기준으로 후보 라인을 확인합니다.
4. 필요한 항목에 노트를 남깁니다.
5. `Export Brief` 또는 `Export Session`으로 Markdown/JSON 산출물을 저장합니다.

## 검증 명령

```powershell
dotnet restore .\RunbookLens.sln
dotnet build .\RunbookLens.sln -c Release --no-restore
dotnet test .\RunbookLens.sln -c Release --no-build
```

macOS/Linux에서 Windows 타깃 빌드만 확인할 때:

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet restore ./RunbookLens.sln
dotnet build ./RunbookLens.sln -c Release --no-restore
dotnet test ./RunbookLens.sln -c Release --no-build
dotnet publish ./src/RunbookLens/RunbookLens.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=false -o artifacts/win-x64
```

> macOS에서 만든 `win-x64` 산출물은 PE 실행 파일 생성까지만 검증합니다. 실제 GUI 실행은 Windows에서 확인해야 합니다.

## 프로젝트 구조

```text
RunbookLens.sln
src/
  RunbookLens/          # WinForms UI
  RunbookLens.Core/     # 로그 스캔/리포트 도메인 로직
tests/
  RunbookLens.Tests/    # xUnit 테스트
```

## 보안/운영 메모

- 로그 분석은 로컬 파일 시스템 안에서만 수행합니다.
- 기본 규칙은 휴리스틱입니다. 운영 정책에 맞춰 규칙/키워드를 조정하세요.
- 대용량 로그는 파일 수와 파일 크기 제한을 둬 UI 멈춤을 줄입니다.
- 내보낸 brief/session 파일에는 로그 미리보기가 포함될 수 있으니 공유 전 민감정보를 확인하세요.

## 릴리즈

- 최신 배포: <https://github.com/tmdry4530/runbook-lens/releases/latest>
- 권장 자산: `RunbookLens-win-x64-<version>.zip`
