using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Overlays;

public sealed partial class VoiceGuideOverlayViewModel : ObservableObject
{
    private readonly Action<int> _applyVolumeLevel;

    public VoiceGuideOverlayViewModel(
        int initialVolumeLevel,
        Action<int> applyVolumeLevel,
        Action stopVoice,
        Func<Task> replayVoiceAsync,
        IRelayCommand closeCommand)
    {
        _applyVolumeLevel = applyVolumeLevel;
        CloseCommand = closeCommand;
        volumeLevel = Math.Clamp(initialVolumeLevel, 1, 5);

        DecreaseVolumeCommand = new RelayCommand(DecreaseVolume);
        IncreaseVolumeCommand = new RelayCommand(IncreaseVolume);
        StopVoiceCommand = new RelayCommand(stopVoice);
        ReplayVoiceCommand = new AsyncRelayCommand(replayVoiceAsync);
        ConfirmCommand = closeCommand;
    }

    [ObservableProperty]
    private int volumeLevel;

    public string Title => "음량 조절";
    public string StopText => "음성정지";
    public string ReplayText => "다시듣기";
    public string ConfirmText => "설정 완료";
    public string VolumeLevelText => VolumeLevel.ToString();

    public bool IsLevel1Active => VolumeLevel >= 1;
    public bool IsLevel2Active => VolumeLevel >= 2;
    public bool IsLevel3Active => VolumeLevel >= 3;
    public bool IsLevel4Active => VolumeLevel >= 4;
    public bool IsLevel5Active => VolumeLevel >= 5;

    public IRelayCommand CloseCommand { get; }
    public IRelayCommand DecreaseVolumeCommand { get; }
    public IRelayCommand IncreaseVolumeCommand { get; }
    public IRelayCommand StopVoiceCommand { get; }
    public IAsyncRelayCommand ReplayVoiceCommand { get; }
    public IRelayCommand ConfirmCommand { get; }

    partial void OnVolumeLevelChanged(int value)
    {
        _applyVolumeLevel(value);
        OnPropertyChanged(nameof(VolumeLevelText));
        OnPropertyChanged(nameof(IsLevel1Active));
        OnPropertyChanged(nameof(IsLevel2Active));
        OnPropertyChanged(nameof(IsLevel3Active));
        OnPropertyChanged(nameof(IsLevel4Active));
        OnPropertyChanged(nameof(IsLevel5Active));
    }

    private void DecreaseVolume()
    {
        VolumeLevel = Math.Max(1, VolumeLevel - 1);
    }

    private void IncreaseVolume()
    {
        VolumeLevel = Math.Min(5, VolumeLevel + 1);
    }
}
