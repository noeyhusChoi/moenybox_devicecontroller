using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Features.ExchangeV2.Orchestration;
using Kiosk.Application.Services.Resx;
using Kiosk.Application.Services.Theme;
using Kiosk.Infrastructure.Initialization;
using Kiosk.Infrastructure.Media;
using Kiosk.Infrastructure.Updates;
using System.ComponentModel;
using Kiosk.ViewModels.Overlays;

namespace Kiosk.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private const double DefaultZoomScale = 1.0;
        private const double ExpandedZoomScale = 1.5;
        private const double WindowWidth = 1080.0;
        private const double WindowHeight = 1920.0;
        private const double UtilityBarHeight = 115.0;
        private const double SurfaceWidth = WindowWidth;
        private const double SurfaceHeight = WindowHeight - UtilityBarHeight;
        private const double MinimapDisplayWidth = 120.0;
        private const double MinimapPadding = 8.0;

        private readonly IAppInitializer _initializer;
        private readonly IHeaderViewModelFactory _headerViewModelFactory;
        private readonly IAppTheme _appTheme;
        private readonly IAudioPlayService _audioPlayService;
        private readonly IAppUpdateService _appUpdateService;
        private bool _initialized;

        [ObservableProperty]
        private string statusMessage = "Ready to initialize retained infrastructure.";

        [ObservableProperty]
        private object? currentScreenViewModel;

        [ObservableProperty]
        private HeaderViewModel headerViewModel = new();

        [ObservableProperty]
        private UtilityBarViewModel utilityBarViewModel = null!;

        [ObservableProperty]
        private object? currentModalViewModel;

        [ObservableProperty]
        private object? currentUtilityOverlayViewModel;

        public bool IsProgressChromeVisible => CurrentScreenViewModel is IProgressChromeShellViewModel shell &&
                                               shell.ShowStepHeader &&
                                               !shell.CollapseShellChrome;

        [ObservableProperty]
        private bool isAccessibilityZoomEnabled;

        [ObservableProperty]
        private double accessibilityZoomScale = DefaultZoomScale;

        [ObservableProperty]
        private double accessibilityPanX;

        [ObservableProperty]
        private double accessibilityPanY;

        private IModalSourceViewModel? _currentModalSource;
        public HomeShellViewModel HomeShell { get; }
        public ExchangeEntryShellViewModel ExchangeEntryShell { get; }
        public CashExchangeShellViewModel CashExchangeShell { get; }
        public PrepaidCardShellViewModel PrepaidCardShell { get; }

        public MainWindowViewModel(
            IAppInitializer initializer,
            IHeaderViewModelFactory headerViewModelFactory,
            IAppTheme appTheme,
            IAudioPlayService audioPlayService,
            IAppUpdateService appUpdateService,
            HomeShellViewModel homeShell,
            ExchangeEntryShellViewModel exchangeEntryShell,
            CashExchangeShellViewModel cashExchangeShell,
            PrepaidCardShellViewModel prepaidCardShell)
        {
            _initializer = initializer;
            _headerViewModelFactory = headerViewModelFactory;
            _appTheme = appTheme;
            _audioPlayService = audioPlayService;
            _appUpdateService = appUpdateService;
            HomeShell = homeShell;
            ExchangeEntryShell = exchangeEntryShell;
            CashExchangeShell = cashExchangeShell;
            PrepaidCardShell = prepaidCardShell;
            HomeShell.ServiceEntryRequested += OnHomeServiceEntryRequested;
            ExchangeEntryShell.HomeRequested += OnExchangeHomeRequested;
            ExchangeEntryShell.CashExchangeRequested += OnCashExchangeRequested;
            ExchangeEntryShell.PrepaidCardRequested += OnPrepaidCardFromEntryRequested;
            ExchangeEntryShell.PropertyChanged += OnProgressShellPropertyChanged;
            CashExchangeShell.HomeRequested += OnExchangeHomeRequested;
            CashExchangeShell.EntryBackRequested += OnCashExchangeEntryBackRequested;
            CashExchangeShell.ExchangeCompleted += OnExchangeCompleted;
            CashExchangeShell.PropertyChanged += OnProgressShellPropertyChanged;
            PrepaidCardShell.HomeRequested += OnExchangeHomeRequested;
            PrepaidCardShell.EntryBackRequested += OnPrepaidCardEntryBackRequested;
            PrepaidCardShell.PropertyChanged += OnProgressShellPropertyChanged;
            _initializer.ProgressChanged += OnProgressChanged;
            _appTheme.ThemeChanged += OnThemeChanged;
            UtilityBarViewModel = new UtilityBarViewModel(
                ShowHome,
                ToggleAccessibilityZoom,
                OpenVoiceGuideOverlay,
                OpenAccessibilitySettings,
                PlaceCall);
            UtilityBarViewModel.SetZoomState(false);
            RefreshUtilityButtonState();
            ShowHome();
        }

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _initialized = true;
            StatusMessage = "Initializing retained infrastructure...";
            await _initializer.InitializeAsync();
            StatusMessage = "Infrastructure initialization complete.";
        }

        private void OnProgressChanged(string message)
        {
            StatusMessage = message;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            RefreshUtilityButtonState();
            HeaderViewModel.LogoAssetPath = _headerViewModelFactory.GetLogoAssetPath();
        }

        private void OnProgressShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IProgressChromeShellViewModel.ShowStepHeader)
                or nameof(IProgressChromeShellViewModel.CollapseShellChrome))
            {
                OnPropertyChanged(nameof(IsProgressChromeVisible));
            }
        }

        private async void OnHomeServiceEntryRequested(object? sender, HomeServiceEntryRequestedEventArgs e)
        {
            switch (e.ServiceType)
            {
                case HomeServiceType.Exchange:
                    await ShowExchangeEntryAsync();
                    break;
                case HomeServiceType.TransportationCard:
                    await ShowPrepaidCardFromHomeAsync();
                    break;
            }
        }

        private async void OnCashExchangeRequested(object? sender, EventArgs e)
        {
            await ShowCashExchangeAsync();
        }

        private async void OnPrepaidCardFromEntryRequested(object? sender, EventArgs e)
        {
            await ShowPrepaidCardFromEntryAsync();
        }

        private async void OnCashExchangeEntryBackRequested(object? sender, EventArgs e)
        {
            await ShowExchangeEntryAtMethodAsync();
        }

        private async void OnPrepaidCardEntryBackRequested(object? sender, EventArgs e)
        {
            await ShowExchangeEntryAtMethodAsync();
        }

        private void OnExchangeHomeRequested(object? sender, EventArgs e)
        {
            ShowHome();
        }

        private void OnExchangeCompleted(object? sender, ExchangeCompletedEventArgs e)
        {
            if (!e.PrintReceipt)
                return;

            ShowHome();
        }

        private void AttachModalSource(object? screenViewModel)
        {
            if (_currentModalSource is not null)
                _currentModalSource.PropertyChanged -= OnModalSourcePropertyChanged;

            _currentModalSource = screenViewModel as IModalSourceViewModel;

            if (_currentModalSource is not null)
                _currentModalSource.PropertyChanged += OnModalSourcePropertyChanged;

            CurrentModalViewModel = _currentModalSource?.CurrentModalViewModel;
        }

        private void OnModalSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IModalSourceViewModel.CurrentModalViewModel))
                return;

            CurrentModalViewModel = _currentModalSource?.CurrentModalViewModel;
        }

        private void ReplaceHeaderViewModel(HeaderViewModel nextViewModel)
        {
            HeaderViewModel.Dispose();
            HeaderViewModel = nextViewModel;
        }

        private void ShowHome()
        {
            HomeShell.ResetToServiceSelection();
            CurrentScreenViewModel = HomeShell;
            _appUpdateService.SetMainIdleState(true);
            AttachModalSource(CurrentScreenViewModel);
            ReplaceHeaderViewModel(_headerViewModelFactory.CreateHomeHeader());
            OnPropertyChanged(nameof(IsProgressChromeVisible));
        }

        private async Task ShowExchangeEntryAsync()
        {
            await ExchangeEntryShell.StartFlowAsync();
            CurrentScreenViewModel = ExchangeEntryShell;
            _appUpdateService.SetMainIdleState(false);
            AttachModalSource(CurrentScreenViewModel);
            ReplaceHeaderViewModel(_headerViewModelFactory.CreateExchangeHeader(ExchangeEntryShell.TimerText));
            OnPropertyChanged(nameof(IsProgressChromeVisible));
        }

        private Task ShowExchangeEntryAtMethodAsync()
        {
            ExchangeEntryShell.ReturnToMethodSelection();
            CurrentScreenViewModel = ExchangeEntryShell;
            _appUpdateService.SetMainIdleState(false);
            AttachModalSource(CurrentScreenViewModel);
            ReplaceHeaderViewModel(_headerViewModelFactory.CreateExchangeHeader(ExchangeEntryShell.TimerText));
            OnPropertyChanged(nameof(IsProgressChromeVisible));
            return Task.CompletedTask;
        }

        private async Task ShowCashExchangeAsync()
        {
            await CashExchangeShell.StartFlowAsync();
            CurrentScreenViewModel = CashExchangeShell;
            _appUpdateService.SetMainIdleState(false);
            AttachModalSource(CurrentScreenViewModel);
            ReplaceHeaderViewModel(_headerViewModelFactory.CreateExchangeHeader(CashExchangeShell.TimerText));
            OnPropertyChanged(nameof(IsProgressChromeVisible));
        }

        private async Task ShowPrepaidCardFromHomeAsync()
        {
            await PrepaidCardShell.StartFlowAsync(PrepaidCardEntrySource.Home);
            CurrentScreenViewModel = PrepaidCardShell;
            _appUpdateService.SetMainIdleState(false);
            AttachModalSource(CurrentScreenViewModel);
            ReplaceHeaderViewModel(_headerViewModelFactory.CreateExchangeHeader(PrepaidCardShell.TimerText));
            OnPropertyChanged(nameof(IsProgressChromeVisible));
        }

        private async Task ShowPrepaidCardFromEntryAsync()
        {
            await PrepaidCardShell.StartFlowAsync(PrepaidCardEntrySource.ExchangeEntry);
            CurrentScreenViewModel = PrepaidCardShell;
            _appUpdateService.SetMainIdleState(false);
            AttachModalSource(CurrentScreenViewModel);
            ReplaceHeaderViewModel(_headerViewModelFactory.CreateExchangeHeader(PrepaidCardShell.TimerText));
            OnPropertyChanged(nameof(IsProgressChromeVisible));
        }

        private void ToggleAccessibilityZoom()
        {
            if (IsAccessibilityZoomEnabled)
            {
                DisableAccessibilityZoom();
                return;
            }

            IsAccessibilityZoomEnabled = true;
            AccessibilityZoomScale = ExpandedZoomScale;
            AccessibilityPanX = 0;
            AccessibilityPanY = 0;
            UtilityBarViewModel.SetZoomState(true);
            NotifyMinimapChanged();
        }

        private void OpenVoiceGuideOverlay()
        {
            CurrentUtilityOverlayViewModel = new VoiceGuideOverlayViewModel(
                GetVoiceGuideVolumeLevel(),
                ApplyVoiceGuideVolumeLevel,
                StopVoiceGuide,
                ReplayVoiceGuideAsync,
                CloseVoiceGuideOverlayCommand);
            RefreshUtilityButtonState();
        }

        private void OpenAccessibilitySettings()
        {
            CurrentUtilityOverlayViewModel = new AccessibilitySettingsOverlayViewModel(
                _appTheme.CurrentTheme,
                ApplyTheme,
                CloseAccessibilitySettingsCommand);
            RefreshUtilityButtonState();
        }

        [RelayCommand]
        private void CloseAccessibilitySettings()
        {
            CurrentUtilityOverlayViewModel = null;
            RefreshUtilityButtonState();
        }

        private void PlaceCall()
        {
        }

        [RelayCommand]
        private void CloseVoiceGuideOverlay()
        {
            CurrentUtilityOverlayViewModel = null;
            RefreshUtilityButtonState();
        }

        private void ApplyTheme(AppThemeKind theme)
        {
            _appTheme.SetTheme(theme);
            RefreshUtilityButtonState();
        }

        private void ApplyVoiceGuideVolumeLevel(int level)
        {
            _audioPlayService.Volume = level / 5f;
        }

        private int GetVoiceGuideVolumeLevel()
        {
            return Math.Clamp(
                (int)Math.Round(_audioPlayService.Volume * 5f, MidpointRounding.AwayFromZero),
                1,
                5);
        }

        private void StopVoiceGuide()
        {
            _audioPlayService.StopAll();
        }

        private Task ReplayVoiceGuideAsync()
        {
            return Task.CompletedTask;
        }

        private void DisableAccessibilityZoom()
        {
            IsAccessibilityZoomEnabled = false;
            AccessibilityZoomScale = DefaultZoomScale;
            AccessibilityPanX = 0;
            AccessibilityPanY = 0;
            UtilityBarViewModel.SetZoomState(false);
            NotifyMinimapChanged();
        }

        public bool CanAccessibilityPan()
        {
            return IsAccessibilityZoomEnabled && AccessibilityZoomScale > DefaultZoomScale;
        }

        public void ApplyAccessibilityPanDelta(double deltaX, double deltaY)
        {
            if (!CanAccessibilityPan())
                return;

            var maxOffsetX = ((SurfaceWidth * AccessibilityZoomScale) - SurfaceWidth) / 2;
            var maxOffsetY = ((SurfaceHeight * AccessibilityZoomScale) - SurfaceHeight) / 2;

            AccessibilityPanX = Clamp(AccessibilityPanX + deltaX, -maxOffsetX, maxOffsetX);
            AccessibilityPanY = Clamp(AccessibilityPanY + deltaY, -maxOffsetY, maxOffsetY);
            NotifyMinimapChanged();
        }

        public double MinimapWidth => MinimapDisplayWidth;

        public double MinimapHeight => MinimapDisplayWidth * SurfaceHeight / SurfaceWidth;

        public double MinimapContentWidth => MinimapWidth - (MinimapPadding * 2);

        public double MinimapContentHeight => MinimapHeight - (MinimapPadding * 2);

        public double MinimapViewportWidth => MinimapContentWidth / AccessibilityZoomScale;

        public double MinimapViewportHeight => MinimapContentHeight / AccessibilityZoomScale;

        public double MinimapViewportLeft
        {
            get
            {
                var sourceLeft = GetViewportLeft();
                return sourceLeft / SurfaceWidth * MinimapContentWidth;
            }
        }

        public double MinimapViewportTop
        {
            get
            {
                var sourceTop = GetViewportTop();
                return sourceTop / SurfaceHeight * MinimapContentHeight;
            }
        }

        private double GetViewportLeft()
        {
            var halfWidth = SurfaceWidth / 2;
            var sourceLeft = halfWidth - ((halfWidth + AccessibilityPanX) / AccessibilityZoomScale);
            return Clamp(sourceLeft, 0, Math.Max(0, SurfaceWidth - (SurfaceWidth / AccessibilityZoomScale)));
        }

        private double GetViewportTop()
        {
            var halfHeight = SurfaceHeight / 2;
            var sourceTop = halfHeight - ((halfHeight + AccessibilityPanY) / AccessibilityZoomScale);
            return Clamp(sourceTop, 0, Math.Max(0, SurfaceHeight - (SurfaceHeight / AccessibilityZoomScale)));
        }

        private void NotifyMinimapChanged()
        {
            OnPropertyChanged(nameof(MinimapWidth));
            OnPropertyChanged(nameof(MinimapHeight));
            OnPropertyChanged(nameof(MinimapContentWidth));
            OnPropertyChanged(nameof(MinimapContentHeight));
            OnPropertyChanged(nameof(MinimapViewportWidth));
            OnPropertyChanged(nameof(MinimapViewportHeight));
            OnPropertyChanged(nameof(MinimapViewportLeft));
            OnPropertyChanged(nameof(MinimapViewportTop));
        }

        private void RefreshUtilityButtonState()
        {
            UtilityBarViewModel.SetAccessibilityState(
                CurrentUtilityOverlayViewModel is AccessibilitySettingsOverlayViewModel ||
                _appTheme.CurrentTheme != AppThemeKind.Light);
            UtilityBarViewModel.SetVoiceGuideState(CurrentUtilityOverlayViewModel is VoiceGuideOverlayViewModel);
        }

        partial void OnAccessibilityZoomScaleChanged(double value)
        {
            NotifyMinimapChanged();
        }

        partial void OnAccessibilityPanXChanged(double value)
        {
            NotifyMinimapChanged();
        }

        partial void OnAccessibilityPanYChanged(double value)
        {
            NotifyMinimapChanged();
        }

        partial void OnIsAccessibilityZoomEnabledChanged(bool value)
        {
            NotifyMinimapChanged();
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;

            return value > max ? max : value;
        }
    }
}
