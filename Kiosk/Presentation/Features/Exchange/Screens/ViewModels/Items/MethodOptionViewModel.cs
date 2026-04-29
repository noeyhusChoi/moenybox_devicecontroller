using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public sealed class MethodOptionViewModel
{
    public MethodOptionViewModel(
        string title,
        string assetPath,
        IAsyncRelayCommand selectCommand)
    {
        Title = title;
        AssetPath = assetPath;
        SelectCommand = selectCommand;
    }

    public string Title { get; }
    public string AssetPath { get; }
    public IAsyncRelayCommand SelectCommand { get; }
}
