using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Services.Resx;
using Kiosk.Application.Services.Theme;
using Kiosk.Infrastructure.Initialization;
using System.ComponentModel;
using System.Windows.Media;
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
        private readonly IAppCulture _appCulture;
        private readonly IAppTheme _appTheme;
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

        [ObservableProperty]
        private FontFamily currentAppFontFamily = null!;

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
        public ExchangeShellViewModel ExchangeShell { get; }

        public MainWindowViewModel(
            IAppInitializer initializer,
            IHeaderViewModelFactory headerViewModelFactory,
            IAppCulture appCulture,
            IAppTheme appTheme,
            HomeShellViewModel homeShell,
            ExchangeShellViewModel exchangeShell)
        {
            _initializer = initializer;
            _headerViewModelFactory = headerViewModelFactory;
            _appCulture = appCulture;
            _appTheme = appTheme;
            HomeShell = homeShell;
            ExchangeShell = exchangeShell;
            HomeShell.ExchangeRequested += OnHomeExchangeRequested;
            ExchangeShell.HomeRequested += OnExchangeHomeRequested;
            _initializer.ProgressChanged += OnProgressChanged;
            _appCulture.CultureChanged += OnCultureChanged;
            _appTheme.ThemeChanged += OnThemeChanged;
            UtilityBarViewModel = new UtilityBarViewModel(ToggleAccessibilityZoom, ToggleKeyboardNavigation, OpenThemeSelector);
            CurrentAppFontFamily = ResolveFontFamily();
            UtilityBarViewModel.SetZoomState(false);
            UtilityBarViewModel.SetAccessibilityState(KeyboardNavigationState.Instance.IsEnabled);
            RefreshThemeButtonState();
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
           
            ShowHome();
        }

        private void OnProgressChanged(string message)
        {
            StatusMessage = message;
        }

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            CurrentAppFontFamily = ResolveFontFamily();
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            RefreshThemeButtonState();
            HeaderViewModel.LogoAssetPath = _headerViewModelFactory.GetLogoAssetPath();
        }

        private async void OnHomeExchangeRequested(object? sender, EventArgs e)
        {
            await ShowExchangeAsync();
        }

        private void OnExchangeHomeRequested(object? sender, EventArgs e)
        {
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
            CurrentScreenViewModel = HomeShell;
            AttachModalSource(CurrentScreenViewModel);
            ReplaceHeaderViewModel(_headerViewModelFactory.CreateHomeHeader());
        }

        private async Task ShowExchangeAsync()
        {
            CurrentScreenViewModel = ExchangeShell;
            AttachModalSource(CurrentScreenViewModel);
            ReplaceHeaderViewModel(_headerViewModelFactory.CreateExchangeHeader(ExchangeShell.TimerText));
            await ExchangeShell.StartFlowAsync();
        }

        private FontFamily ResolveFontFamily()
        {
            var resourceKey = _appCulture.CurrentCulture.Name switch
            {
                "ko-KR" => "Noto Sans KR",
                "ja-JP" => "Noto Sans JP",
                "zh-CN" => "Noto Sans SC",
                "zh-TW" => "Noto Sans TC",
                _ => "Noto Sans",
            };

            return (FontFamily)System.Windows.Application.Current.Resources[resourceKey];
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

        private void ToggleKeyboardNavigation()
        {
            KeyboardNavigationState.Instance.IsEnabled = !KeyboardNavigationState.Instance.IsEnabled;
            UtilityBarViewModel.SetAccessibilityState(KeyboardNavigationState.Instance.IsEnabled);
        }

        private void OpenThemeSelector()
        {
            CurrentUtilityOverlayViewModel = new ThemeSelectionOverlayViewModel(
                _appTheme.CurrentTheme,
                ApplyTheme,
                CloseThemeSelectorCommand);
            RefreshThemeButtonState();
        }

        [RelayCommand]
        private void CloseThemeSelector()
        {
            CurrentUtilityOverlayViewModel = null;
            RefreshThemeButtonState();
        }

        private void ApplyTheme(AppThemeKind theme)
        {
            _appTheme.SetTheme(theme);
            CloseThemeSelector();
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

        private void RefreshThemeButtonState()
        {
            UtilityBarViewModel.SetThemeState(
                CurrentUtilityOverlayViewModel is ThemeSelectionOverlayViewModel ||
                _appTheme.CurrentTheme != AppThemeKind.Light);
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
