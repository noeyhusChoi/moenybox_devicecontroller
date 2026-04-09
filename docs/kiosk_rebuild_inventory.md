# Kiosk Rebuild Inventory

## Purpose

이 문서는 `codex/rebuild-service-flows` 브랜치에서
실제 정리 작업을 수행하기 위한 파일/폴더 단위 inventory다.

기준 문서:

- `docs/kiosk_rebuild_scope.md`

## Keep As-Is

### Solution / App bootstrap

- `Moneybox.sln`
- `KIOSK/App.xaml`
- `KIOSK/App.xaml.cs`

### Device low-level core

- `DeviceKit/**`

### Infrastructure - database / cache / API / OCR / media / hosting / init

- `KIOSK/Infrastructure/API/**`
- `KIOSK/Infrastructure/Cache/**`
- `KIOSK/Infrastructure/Database/**`
- `KIOSK/Infrastructure/Hosting/AppBootstrapper.cs`
- `KIOSK/Infrastructure/Hosting/HostController.cs`
- `KIOSK/Infrastructure/Hosting/IHostController.cs`
- `KIOSK/Infrastructure/Hosting/Modules/ApiModule.cs`
- `KIOSK/Infrastructure/Hosting/Modules/DatabaseModule.cs`
- `KIOSK/Infrastructure/Hosting/Modules/HostModule.cs`
- `KIOSK/Infrastructure/Hosting/Modules/LoggingModule.cs`
- `KIOSK/Infrastructure/Hosting/Modules/OcrModule.cs`
- `KIOSK/Infrastructure/Hosting/Modules/PlatformModule.cs`
- `KIOSK/Infrastructure/Initialization/**`
- `KIOSK/Infrastructure/Logging/**`
- `KIOSK/Infrastructure/Media/**`
- `KIOSK/Infrastructure/OCR/**`
- `KIOSK/Infrastructure/Configuration/**`
- `KIOSK/Infrastructure/Common/**`

### Application contracts and reusable wrappers

- `KIOSK/Application/Abstractions/**`
- `KIOSK/Application/Services/API/CemsApiClientService.cs`
- `KIOSK/Application/Services/API/GtfApiClientService.cs`
- `KIOSK/Application/Services/DataBase/WithdrawalCassetteService.cs`
- `KIOSK/Application/Services/DataBase/WithdrawalCassetteServiceV2.cs`
- `KIOSK/Application/Services/Transactions/TransactionOutboxService.cs`
- `KIOSK/Application/Services/Transactions/ITransactionOutboxService.cs`

### Presentation controls only

- `KIOSK/Presentation/Controls/LottiePlayerControl.cs`
- `KIOSK/Presentation/Controls/VideoPlayerControl.cs`

## Replace Completely

### Presentation

- `KIOSK/Presentation/Abstractions/**`
- `KIOSK/Presentation/Features/**`
- `KIOSK/Presentation/Layouts/**`
- `KIOSK/Presentation/Navigation/**`
- `KIOSK/Presentation/Shared/**`
- `KIOSK/Presentation/Window/**`
- `KIOSK/Presentation/Controls/WebpPlayerControl.cs`

의도:

- 화면, 네비게이션, 윈도우, 공통 ViewModel 베이스, 기능 화면을 새 구조로 다시 만든다.

### Kiosk device integration layer

- `KIOSK/Infrastructure/Devices/**`
- `KIOSK/Application/Services/Devices/IDeviceCommandService.cs`
- `KIOSK/Application/Services/DeviceErrorEventService.cs`

의도:

- 장치 통합 계층은 `DeviceKit` 위에 새로 설계한다.

### Platform helpers not kept

- `KIOSK/Infrastructure/Storage/**`
- `KIOSK/Infrastructure/Network/**`

의도:

- 새 앱 구조에서 필요한 경우에만 다시 추가한다.

### Legacy business flows

- `KIOSK/Application/Services/Exchange/**`
- `KIOSK/Application/Features/ExchangeV2/**`
- `KIOSK/Application/Services/GtfTaxRefundService.cs`
- `KIOSK/Application/StateMachines/ExchangeSellStateMachine.cs`
- `KIOSK/Application/StateMachines/GtfStateMachine.cs`

의도:

- 기존 `exchange`, `exchangev2`, `gtf` 워크플로우 제거

### Legacy business/session models

- `KIOSK/Application/Services/TransactionService.cs`
- `KIOSK/Application/Services/ReceiptPrintService.cs`

의도:

- 거래 세션 모델과 영수증 포맷도 신규 서비스 설계 기준으로 재작성

### DI and composition tied to old structure

- `KIOSK/CompositionRoot/Modules/AppServicesModule.cs`
- `KIOSK/CompositionRoot/Modules/StateMachineModule.cs`
- `KIOSK/CompositionRoot/Modules/UiModule.cs`
- `KIOSK/CompositionRoot/Modules/ViewModelModule.cs`
- `KIOSK/CompositionRoot/Modules/WindowModule.cs`
- `KIOSK/CompositionRoot/Modules/BootstrapExtensions.cs`

의도:

- 기존 Exchange/GTF/ViewModel 등록을 제거하고
  새 구조에 맞는 DI 등록 체계로 다시 만든다.

### Background jobs tied to old business

- `KIOSK/Infrastructure/Hosting/Modules/BackgroundModule.cs`
- `KIOSK/Application/Services/BackgroundTaskService.cs`
- `KIOSK/Application/Services/BackgroundTasks/**`

의도:

- 기존 환율 갱신/거래 송신 background task는 새 서비스 기준으로 재설계

## Review One-by-One

### Domain

- `KIOSK/Domain/**`

판단 기준:

- 장치/설정/공용 인프라 모델이면 유지 가능
- 거래, 환전, 환급, 고객 세션 중심 모델이면 제거 또는 교체

### Models

- `KIOSK/Models/**`

판단 기준:

- 새 UI 또는 인프라에서 재사용 가능한지 확인 후 유지 여부 결정

### Application common services

- `KIOSK/Application/Services/Localization/**`
- `KIOSK/Application/Services/Resx/**`
- `KIOSK/Application/Services/InactivityService.cs`
- `KIOSK/Application/Services/QrGenerateService.cs`
- `KIOSK/Application/Services/UiDispatcher.cs`
- `KIOSK/Application/Services/Health/**`

판단 기준:

- 새 UI와 공통 인프라에서 필요하면 유지
- 기존 흐름에 종속되면 제거

### Application state machine base

- `KIOSK/Application/StateMachines/WorkflowStateMachine.cs`
- `KIOSK/Application/StateMachines/StateMachineTrigger.cs`

판단 기준:

- 새 서비스가 상태 머신 기반이면 유지
- 아니면 제거

## First Cleanup Pass

1. `CompositionRoot`에서 기존 Exchange/ExchangeV2/GTF 관련 DI 등록 제거
2. `BackgroundModule` 제거 또는 비활성화
3. `Presentation`을 신규 셸 기준으로 재구성할 수 있게 기존 feature 폴더 제거
4. `Infrastructure/Devices`와 기존 device service 계약 제거
5. 새 `DeviceKit` adapter 계층 추가
6. 새 `Presentation` 최소 골격 생성

## Minimal Build Target After Cleanup

1차 목표는 기능 동작이 아니라 다음 상태다.

- 앱이 부팅된다
- 기본 MainWindow가 뜬다
- DI가 정상 구성된다
- DB/API/OCR/Audio/DeviceKit 기반 서비스가 등록된다
- 기존 Exchange/GTF 화면과 상태 머신은 더 이상 연결되지 않는다

## Notes

- 현재 브랜치에는 기존 사용자 변경사항이 이미 포함되어 있을 수 있다.
- 실제 삭제 전에는 `git status`로 다른 작업과 충돌 여부를 확인한다.
- 큰 삭제는 단계별 커밋으로 분리한다.
