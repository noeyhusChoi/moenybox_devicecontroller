using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public sealed class CurrencyOptionViewModel
{
    public CurrencyOptionViewModel(
        string code,
        string rateText,
        string assetPath,
        IAsyncRelayCommand selectCommand)
    {
        Code = code;
        RateText = rateText;
        AssetPath = assetPath;
        SelectCommand = selectCommand;
    }

    public string Code { get; }
    public string RateText { get; }
    public string AssetPath { get; }
    public IAsyncRelayCommand SelectCommand { get; }
}
