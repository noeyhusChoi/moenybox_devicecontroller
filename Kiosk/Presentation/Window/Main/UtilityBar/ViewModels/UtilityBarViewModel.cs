using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels;

public partial class UtilityBarViewModel : ObservableObject
{
    public UtilityBarViewModel(
        Action goHome,
        Action toggleZoom,
        Action openVoiceGuideSettings,
        Action openAccessibilitySettings,
        Action placeCall)
    {
        HomeCommand = new RelayCommand(goHome);
        ZoomCommand = new RelayCommand(toggleZoom);
        VoiceGuideCommand = new RelayCommand(openVoiceGuideSettings);
        AccessibilityCommand = new RelayCommand(openAccessibilitySettings);
        CallCommand = new RelayCommand(placeCall);
    }

    public IRelayCommand HomeCommand { get; }
    public IRelayCommand ZoomCommand { get; }
    public IRelayCommand VoiceGuideCommand { get; }
    public IRelayCommand AccessibilityCommand { get; }
    public IRelayCommand CallCommand { get; }

    [ObservableProperty]
    private bool isZoomSelected;

    [ObservableProperty]
    private bool isVoiceGuideSelected;

    [ObservableProperty]
    private bool isAccessibilitySelected;

    public void SetZoomState(bool isZoomed)
    {
        IsZoomSelected = isZoomed;
    }

    public void SetVoiceGuideState(bool isActive)
    {
        IsVoiceGuideSelected = isActive;
    }

    public void SetAccessibilityState(bool isActive)
    {
        IsAccessibilitySelected = isActive;
    }
}
