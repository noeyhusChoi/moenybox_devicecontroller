using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Services.Theme;

namespace Kiosk.ViewModels.Overlays;

public sealed partial class AccessibilitySettingsOverlayViewModel : ObservableObject
{
    private readonly Action<AppThemeKind> _applyTheme;

    public AccessibilitySettingsOverlayViewModel(
        AppThemeKind currentTheme,
        Action<AppThemeKind> applyTheme,
        IRelayCommand closeCommand)
    {
        _applyTheme = applyTheme;
        CloseCommand = closeCommand;
        isLowScreenEnabled = false;
        isHighContrastEnabled = currentTheme == AppThemeKind.HighContrast;

        SelectLowScreenOnCommand = new RelayCommand(() => IsLowScreenEnabled = true);
        SelectLowScreenOffCommand = new RelayCommand(() => IsLowScreenEnabled = false);
        SelectHighContrastOnCommand = new RelayCommand(() => SetHighContrast(true));
        SelectHighContrastOffCommand = new RelayCommand(() => SetHighContrast(false));
        ResetCommand = new RelayCommand(ResetSettings);
        ConfirmCommand = closeCommand;
    }

    [ObservableProperty]
    private bool isLowScreenEnabled;

    [ObservableProperty]
    private bool isHighContrastEnabled;

    public IRelayCommand CloseCommand { get; }
    public IRelayCommand SelectLowScreenOnCommand { get; }
    public IRelayCommand SelectLowScreenOffCommand { get; }
    public IRelayCommand SelectHighContrastOnCommand { get; }
    public IRelayCommand SelectHighContrastOffCommand { get; }
    public IRelayCommand ResetCommand { get; }
    public IRelayCommand ConfirmCommand { get; }

    private void ResetSettings()
    {
        IsLowScreenEnabled = false;
        SetHighContrast(false);
    }

    private void SetHighContrast(bool isEnabled)
    {
        IsHighContrastEnabled = isEnabled;
        _applyTheme(isEnabled ? AppThemeKind.HighContrast : AppThemeKind.Light);
    }
}
