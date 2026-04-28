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
        SelectDefaultCommand = new RelayCommand(() => applyTheme(AppThemeKind.Light));
        SelectHighContrastCommand = new RelayCommand(() => applyTheme(AppThemeKind.HighContrast));
    }

    public AppThemeKind CurrentTheme { get; }
    public IRelayCommand CloseCommand { get; }
    public IRelayCommand SelectDefaultCommand { get; }
    public IRelayCommand SelectHighContrastCommand { get; }

    public bool IsDefaultSelected => CurrentTheme == AppThemeKind.Light;
    public bool IsHighContrastSelected => CurrentTheme == AppThemeKind.HighContrast;
}
