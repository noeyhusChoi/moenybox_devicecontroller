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

    public string Title => "접근성 설정하기";
    public string LowScreenLabel => "낮은 화면";
    public string HighContrastLabel => "고대비";
    public string OnText => "켜기";
    public string OffText => "끄기";
    public string ResetText => "초기화";
    public string ConfirmText => "설정 완료";

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
