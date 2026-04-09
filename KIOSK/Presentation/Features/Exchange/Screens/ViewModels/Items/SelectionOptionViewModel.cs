using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public sealed class SelectionOptionViewModel
{
    public SelectionOptionViewModel(
        string title,
        string? subtitle,
        IAsyncRelayCommand selectCommand,
        string? assetPath = null,
        string? badgeKey = null)
    {
        Title = title;
        Subtitle = subtitle;
        SelectCommand = selectCommand;
        AssetPath = assetPath;
        BadgeKey = badgeKey;
    }

    public string Title { get; }
    public string? Subtitle { get; }
    public IAsyncRelayCommand SelectCommand { get; }
    public string? AssetPath { get; }
    public string? BadgeKey { get; }
    public bool UseAssetImage => !string.IsNullOrWhiteSpace(AssetPath);
}
