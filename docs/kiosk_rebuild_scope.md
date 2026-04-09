# Kiosk Rebuild Scope

## Goal

기존 Kiosk 앱에서 업무 플로우를 제거하고, 아래 기반 계층만 유지한 상태에서
새 화면, 새 네비게이션, 새 서비스 로직, 새 장비 제어 오케스트레이션을 다시 구축한다.

핵심 원칙:

- 유지: 기반 기술, 공용 인프라, 장치/DB/API/OCR 연결 계층
- 교체: 환전/환급 비즈니스 플로우, 화면 흐름, 상태 머신, 화면별 장치 제어 시나리오

## Keep

### 1. Database and Cache

유지 목적:

- 장치 설정
- 키오스크 설정
- 공통 마스터 데이터
- 장치 상태/로그/구성 저장

유지 대상:

- `KIOSK/Infrastructure/Database`
- `KIOSK/Infrastructure/Cache`
- EF Core `DbContext`, Entity, Repository, SQL, MySQL 연동 코드
- 설정/기기/카세트 등 인프라성 DB 접근 서비스

조건부 유지:

- `KIOSK/Application/Services/DataBase`
- `KIOSK/Application/Services/Transactions/TransactionOutboxService.cs`

주의:

- 거래 비즈니스 모델 저장 로직이 새 서비스 설계와 맞지 않으면 내부 구현은 교체하되,
  EF/MySQL 연결 방식과 저장소 구조는 최대한 재사용한다.

### 2. Audio

유지 대상:

- `KIOSK/Infrastructure/Media`

### 3. API

유지 대상:

- `KIOSK/Infrastructure/API`
- `KIOSK/Application/Services/API`

원칙:

- API Envelope, Client, Builder, DTO, Retry 정책은 유지
- 어떤 화면/서비스가 어떤 API를 언제 호출하는지는 새로 작성

### 4. Shared Media Controls

유지 대상:

- `KIOSK/Presentation/Controls/LottiePlayerControl.cs`
- `KIOSK/Presentation/Controls/VideoPlayerControl.cs`

### 5. OCR

유지 대상:

- `KIOSK/Infrastructure/OCR`

원칙:

- OCR 엔진과 결과 파싱 파이프라인은 유지
- OCR을 어느 플로우에서 어떻게 사용할지는 새 서비스에서 다시 정의

### 6. Core App Bootstrap

유지 대상:

- `KIOSK/App.xaml`
- `KIOSK/App.xaml.cs`
- `KIOSK/Infrastructure/Hosting`
- `KIOSK/Infrastructure/Initialization`

원칙:

- 앱 부팅, DI 구성, 초기화 파이프라인만 유지
- `Presentation` 계층의 셸, 윈도우, 네비게이션, 공통 베이스 클래스는 유지 대상으로 보지 않는다
- 새 UI 구조에 맞춰 `Presentation`은 다시 설계한다

### 7. DeviceKit-based Low-level Device Stack

유지 대상:

- `DeviceKit`

원칙:

- 저수준 장치 드라이버, 프로토콜, 런타임 포트는 `DeviceKit`에서 재사용
- KIOSK 내부 장치 통합 계층은 유지 대상으로 보지 않는다
- 장치 명령 래퍼, 장치 상태 브리지, 장치 제어 서비스는 새 구조로 다시 작성

즉:

- 유지: "`DeviceKit`이 제공하는 장치와 통신하는 기반"
- 교체: "KIOSK가 장치를 감싸고 호출하는 방식 전체"

## Replace

### 1. Exchange Legacy Flow

교체 대상:

- `KIOSK/Application/Services/Exchange`
- `KIOSK/Presentation/Features/Exchange`
- `KIOSK/Application/StateMachines/ExchangeSellStateMachine.cs`

### 2. ExchangeV2 Flow

교체 대상:

- `KIOSK/Application/Features/ExchangeV2`
- `KIOSK/Presentation/Features/ExchangeV2`

### 3. GTF Flow

교체 대상:

- `KIOSK/Presentation/Features/GTF`
- `KIOSK/Application/Services/GtfTaxRefundService.cs`
- `KIOSK/Application/StateMachines/GtfStateMachine.cs`

### 4. Legacy Business Session Models

교체 후보:

- `KIOSK/Application/Services/TransactionService.cs`
- 기존 `TransactionModelV2` 중심의 화면-업무 결합 구조

원칙:

- 새 서비스에서 필요한 세션 모델, 플로우 모델, 결과 모델을 다시 정의

### 5. Kiosk Device Integration Layer

교체 대상:

- `KIOSK/Infrastructure/Devices`
- `KIOSK/Application/Services/Devices/IDeviceCommandService.cs`
- 기존 화면/ViewModel에서 직접 장치 명령을 호출하는 코드

원칙:

- 새 장치 계층은 `DeviceKit` 위에 다시 설계한다
- 장치별 adapter, facade, scenario service를 명확히 분리한다
- 기존 장치 식별자 문자열과 명령 흐름도 재정의 가능 대상으로 본다

### 6. Platform Helpers Not Kept

교체 대상:

- `KIOSK/Infrastructure/Storage`
- `KIOSK/Infrastructure/Network`

원칙:

- 기존 플랫폼 보조 계층은 유지 대상으로 보지 않는다
- 필요 시 새 구조 기준으로 다시 설계한다

## Conditional Keep

### Workflow Base

조건부 유지:

- `KIOSK/Application/StateMachines/WorkflowStateMachine.cs`
- `KIOSK/Application/StateMachines/StateMachineTrigger.cs`

유지 조건:

- 새 서비스도 상태 머신 패턴을 채택할 경우

제거 조건:

- 새 구조가 단순 라우터 + 유스케이스 방식이면 제거 가능

### Receipt

조건부 유지:

- `KIOSK/Application/Services/ReceiptPrintService.cs`

유지 조건:

- 프린터 출력 포맷을 일부 재사용할 경우

제거 조건:

- 영수증 포맷과 출력 시나리오를 새로 설계할 경우

## New Development Direction

새로 만들 영역:

- 새 화면 구조
- 새 네비게이션 구조
- 새 서비스 계층
- 새 플로우 오케스트레이션
- DeviceKit 기반 신규 장치 통합 계층
- `Presentation` 전체 구조
- 메인 윈도우, 레이아웃, 공통 뷰모델 베이스, 기능별 화면

권장 계층:

- `Application/UseCases` 또는 `Application/Flows`
- `Application/Devices`
- `Presentation/Features/<NewFeature>`

권장 원칙:

- ViewModel은 화면 상태와 명령만 가진다
- 장비 호출은 DeviceKit 기반 장비 서비스로 위임한다
- OCR/API/DB는 유스케이스에서 조합한다
- 특정 기능명(`exchange`, `exchangev2`, `gtf`)에 종속된 서비스명은 더 이상 사용하지 않는다

## Cleanup Order

1. DI 등록에서 기존 Exchange, ExchangeV2, GTF 관련 항목 분리
2. 기존 화면 진입 경로를 placeholder 또는 새 메뉴 구조로 대체
3. 기존 업무 플로우 폴더 제거
4. 새 장비 서비스 계층 추가
5. 새 화면과 새 네비게이션 플로우 연결

## Branch

작업 브랜치:

- `codex/rebuild-service-flows`

## Current Status

이 문서는 "무엇을 남기고 무엇을 교체할지"를 고정하기 위한 기준 문서다.
실제 삭제/정리 작업은 이 기준에 따라 별도 커밋으로 진행한다.
