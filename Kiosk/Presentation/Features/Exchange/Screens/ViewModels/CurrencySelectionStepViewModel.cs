namespace Kiosk.ViewModels.Steps;

public sealed class CurrencySelectionStepViewModel : ExchangeStepViewModelBase
{
    public CurrencySelectionStepViewModel(IReadOnlyList<CurrencyOptionViewModel> options)
    {
        Options = options;
    }

    public IReadOnlyList<CurrencyOptionViewModel> Options { get; }
}
