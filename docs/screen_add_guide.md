# 화면 추가 가이드

목적
- `KIOSK`에 새 화면을 추가할 때 필요한 등록 지점을 한 번에 확인하기 위한 문서다.
- 현재 프로젝트 구조는 `Layout + Page + FlowCoordinator + NavigationService + DataTemplate` 조합으로 동작한다.

적용 범위
- `Environment`
- `Menu`, `MenuV2`
- `Exchange`, `ExchangeV2`
- `GTF`
- `Startup`

## 1. 먼저 결정할 것

새 화면을 추가하기 전에 아래를 먼저 정한다.

- 어떤 기능 아래에 들어가는가
  - 예: `Exchange`, `GTF`, `Menu`
- 화면 타입이 무엇인가
  - `Layout`: 기능 전체 껍데기
  - `Page`: 실제 한 단계 화면
  - `Popup`: 오버레이/팝업
- 화면 전환 주체가 무엇인가
  - 상태머신/플로우 코디네이터
  - 특정 커맨드에서 직접 `INavigationService`

일반적으로는 새 기능 전체를 추가하는 게 아니면 `Page` 추가가 가장 많다.

## 2. 폴더 위치

기능별 기본 위치는 아래를 따른다.

- Layout ViewModel
  - `KIOSK/Presentation/Features/<Feature>/Layout/ViewModels`
- Layout View
  - `KIOSK/Presentation/Features/<Feature>/Layout/Views`
- Page ViewModel
  - `KIOSK/Presentation/Features/<Feature>/Pages/ViewModels`
- Page View
  - `KIOSK/Presentation/Features/<Feature>/Pages/Views`
- Popup ViewModel
  - `KIOSK/Presentation/Features/<Feature>/Popup/ViewModels`
- Popup View
  - `KIOSK/Presentation/Features/<Feature>/Popup/Views`

예시
- 환전 통화 선택 ViewModel
  - [KIOSK/Presentation/Features/Exchange/Pages/ViewModels/ExchangeCurrenyViewModel.cs](/c:/Users/niaci/RiderProjects/moenybox_devicecontroller/KIOSK/Presentation/Features/Exchange/Pages/ViewModels/ExchangeCurrenyViewModel.cs)
- 환전 통화 선택 View
  - [KIOSK/Presentation/Features/Exchange/Pages/Views/ExchangeCurrencyView.xaml](/c:/Users/niaci/RiderProjects/moenybox_devicecontroller/KIOSK/Presentation/Features/Exchange/Pages/Views/ExchangeCurrencyView.xaml)

## 3. ViewModel 작성

대부분의 단계형 화면은 `PageViewModelBase`를 상속한다.

기준 파일
- [KIOSK/Presentation/Abstractions/PageViewModelBase.cs](/c:/Users/niaci/RiderProjects/moenybox_devicecontroller/KIOSK/Presentation/Abstractions/PageViewModelBase.cs)

필수 구현
- `OnLoadAsync(object? parameter, CancellationToken ct)`
- `OnUnloadAsync()`

`PageViewModelBase`가 제공하는 것
- `OnStepMain`
- `OnStepPrevious`
- `OnStepNext`
- `OnStepError`
- `ExecuteStepAsync(...)`

즉 개별 화면은 아래 두 가지에 집중하면 된다.

- 화면 고유 데이터 로드
- 버튼/선택 커맨드 정의

간단한 예시

```csharp
public partial class ExchangeExampleViewModel : PageViewModelBase
{
    [ObservableProperty]
    private string title = string.Empty;

    public override Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        Title = "Example";
        return Task.CompletedTask;
    }

    public override Task OnUnloadAsync() => Task.CompletedTask;

    [RelayCommand]
    private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

    [RelayCommand]
    private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

    [RelayCommand]
    private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);
}
```

## 4. View 작성

View는 기능 폴더의 `Pages/Views` 또는 `Popup/Views` 아래에 만든다.

권장 원칙
- View는 UI 배치와 바인딩만 담당
- 비즈니스 판단은 ViewModel로 보낸다
- 버튼은 `RelayCommand`에 바인딩한다

최소 예시

```xml
<UserControl
    x:Class="Kiosk.Presentation.Features.Exchange.Pages.Views.ExchangeExampleView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Grid>
        <StackPanel>
            <TextBlock Text="{Binding Title}" />
            <Button Command="{Binding PreviousCommand}" Content="이전" />
            <Button Command="{Binding NextCommand}" Content="다음" />
        </StackPanel>
    </Grid>
</UserControl>
```

## 5. DI 등록

새 ViewModel은 DI에 등록해야 한다.

등록 파일
- [KIOSK/CompositionRoot/Modules/ViewModelModule.cs](/c:/Users/niaci/RiderProjects/moenybox_devicecontroller/KIOSK/CompositionRoot/Modules/ViewModelModule.cs)

기준
- `Layout`은 보통 `AddScoped`
- `Page`, `Popup`은 보통 `AddTransient`

예시

```csharp
services.AddTransient<ExchangeExampleViewModel>();
```

## 6. DataTemplate 등록

ViewModel을 화면으로 렌더링하려면 `MainWindowView.xaml`에 DataTemplate를 추가해야 한다.

등록 파일
- [KIOSK/Presentation/Window/Main/Views/MainWindowView.xaml](/c:/Users/niaci/RiderProjects/moenybox_devicecontroller/KIOSK/Presentation/Window/Main/Views/MainWindowView.xaml)

예시

```xml
<DataTemplate DataType="{x:Type exchangeViewModels:ExchangeExampleViewModel}">
    <exchangeViews:ExchangeExampleView />
</DataTemplate>
```

빠뜨리면 발생하는 문제
- ViewModel은 생성되지만 화면에 아무 것도 안 보임
- `ContentControl`이 타입을 몰라 렌더링하지 못함

## 7. 화면 진입 경로 연결

화면을 만들기만 해서는 보이지 않는다. 진입 경로를 연결해야 한다.

주로 수정하는 위치는 아래 중 하나다.

- 플로우 코디네이터
  - 예: [KIOSK/Presentation/Features/Exchange/Flow/ExchangeFlowCoordinator.cs](/c:/Users/niaci/RiderProjects/moenybox_devicecontroller/KIOSK/Presentation/Features/Exchange/Flow/ExchangeFlowCoordinator.cs)
- 상태머신
- 특정 커맨드 내부에서 `INavigationService.NavigatePage<T>()`

예시

```csharp
ExchangeState.Example => _nav.NavigatePage<ExchangeExampleViewModel>(vm =>
    BindDefaultHandlers(vm)),
```

즉 화면 추가 시 아래 둘 중 하나는 반드시 있어야 한다.

- 상태 전이에서 새 화면으로 가는 분기
- 기존 화면 버튼이 새 화면으로 가는 커맨드

## 8. 파라미터 전달

화면 진입 시 값이 필요하면 `NavigatePage<T>(..., parameter)`를 사용한다.

기준 파일
- [KIOSK/Presentation/Navigation/Services/NavigationService.cs](/c:/Users/niaci/RiderProjects/moenybox_devicecontroller/KIOSK/Presentation/Navigation/Services/NavigationService.cs)

예시

```csharp
await _nav.NavigatePage<ExchangeExampleViewModel>(vm =>
{
    BindDefaultHandlers(vm);
}, parameter: selectedCurrency);
```

받는 쪽은 `OnLoadAsync(object? parameter, ...)`에서 처리한다.

```csharp
public override Task OnLoadAsync(object? parameter, CancellationToken ct)
{
    if (parameter is string currencyCode)
    {
        // use parameter
    }

    return Task.CompletedTask;
}
```

## 9. 팝업 추가 시 추가 확인

팝업도 기본 흐름은 같다.

- Popup ViewModel 작성
- Popup View 작성
- `ViewModelModule`에 등록
- `MainWindowView.xaml` DataTemplate 등록
- 호출 위치에서 `CurrentPopup` 또는 관련 네비게이션 경로 연결

기존 예시
- [KIOSK/Presentation/Features/Exchange/Popup/ViewModels/ExchangePopupTermsViewModel.cs](/c:/Users/niaci/RiderProjects/moenybox_devicecontroller/KIOSK/Presentation/Features/Exchange/Popup/ViewModels/ExchangePopupTermsViewModel.cs)
- [KIOSK/Presentation/Features/Exchange/Popup/Views/ExchangePopupTermsView.xaml](/c:/Users/niaci/RiderProjects/moenybox_devicecontroller/KIOSK/Presentation/Features/Exchange/Popup/Views/ExchangePopupTermsView.xaml)

## 10. 체크리스트

새 화면 추가 후 아래를 확인한다.

- ViewModel 파일 생성
- View 파일 생성
- `ViewModelModule` 등록
- `MainWindowView.xaml` DataTemplate 등록
- 플로우 코디네이터 또는 커맨드 경로 연결
- `OnLoadAsync` 구현
- 이전/다음/메인 버튼 연결 여부 확인
- 빌드 확인

권장 명령

```bash
dotnet build DeviceController.sln -c Debug
```

## 11. 자주 놓치는 포인트

- ViewModel만 만들고 DataTemplate 등록을 안 함
- DI 등록을 안 해서 런타임에 resolve 실패
- 상태머신/코디네이터 분기를 안 넣어서 화면 진입 불가
- `OnLoadAsync`에서 파라미터 형변환을 잘못함
- `PageViewModelBase`의 공통 핸들러를 안 묶어서 이전/다음이 동작하지 않음

## 12. 권장 작업 순서

가장 안전한 순서는 아래다.

1. ViewModel 생성
2. View 생성
3. DI 등록
4. DataTemplate 등록
5. 플로우 연결
6. `dotnet build`
7. 실행 후 실제 진입 확인

이 순서를 따르면 런타임 오류 지점을 가장 빨리 좁힐 수 있다.
