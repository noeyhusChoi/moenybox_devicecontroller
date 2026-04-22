using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels;

public partial class UtilityBarViewModel : ObservableObject
{
    public UtilityBarViewModel(Action goHome, Action toggleZoom, Action toggleKeyboardNavigation, Action openThemeSelector)
    {
        HomeCommand = new RelayCommand(goHome);
        ZoomCommand = new RelayCommand(toggleZoom);
        VoiceGuideCommand = new RelayCommand(() => { });
        AccessibilityCommand = new RelayCommand(toggleKeyboardNavigation);
        ThemeCommand = new RelayCommand(openThemeSelector);
    }

    public IRelayCommand HomeCommand { get; }
    public IRelayCommand ZoomCommand { get; }
    public IRelayCommand VoiceGuideCommand { get; }
    public IRelayCommand AccessibilityCommand { get; }
    public IRelayCommand ThemeCommand { get; }

    [ObservableProperty]
    private bool isZoomSelected;

    [ObservableProperty]
    private bool isThemeSelected;

    [ObservableProperty]
    private bool isAccessibilitySelected;

    public void SetZoomState(bool isZoomed)
    {
        IsZoomSelected = isZoomed;
    }

    public void SetThemeState(bool isActive)
    {
        IsThemeSelected = isActive;
    }

    public void SetAccessibilityState(bool isActive)
    {
        IsAccessibilitySelected = isActive;
    }
}
