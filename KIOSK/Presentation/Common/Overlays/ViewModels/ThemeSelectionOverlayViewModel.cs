using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Services.Theme;

namespace Kiosk.ViewModels.Overlays;

public sealed class ThemeSelectionOverlayViewModel
{
    public ThemeSelectionOverlayViewModel(
        AppThemeKind currentTheme,
        Action<AppThemeKind> applyTheme,
        IRelayCommand closeCommand)
    {
        CurrentTheme = currentTheme;
        CloseCommand = closeCommand;
        SelectBlueCommand = new RelayCommand(() => applyTheme(AppThemeKind.Light));
        SelectRedCommand = new RelayCommand(() => applyTheme(AppThemeKind.LightRed));
        SelectOrangeCommand = new RelayCommand(() => applyTheme(AppThemeKind.LightOrange));
        SelectGreenCommand = new RelayCommand(() => applyTheme(AppThemeKind.LightGreen));
        SelectDarkCommand = new RelayCommand(() => applyTheme(AppThemeKind.Black));
    }

    public AppThemeKind CurrentTheme { get; }
    public string Title => "테마 선택";
    public IRelayCommand CloseCommand { get; }
    public IRelayCommand SelectBlueCommand { get; }
    public IRelayCommand SelectRedCommand { get; }
    public IRelayCommand SelectOrangeCommand { get; }
    public IRelayCommand SelectGreenCommand { get; }
    public IRelayCommand SelectDarkCommand { get; }

    public bool IsBlueSelected => CurrentTheme == AppThemeKind.Light;
    public bool IsRedSelected => CurrentTheme == AppThemeKind.LightRed;
    public bool IsOrangeSelected => CurrentTheme == AppThemeKind.LightOrange;
    public bool IsGreenSelected => CurrentTheme == AppThemeKind.LightGreen;
    public bool IsDarkSelected => CurrentTheme == AppThemeKind.Black;
}
