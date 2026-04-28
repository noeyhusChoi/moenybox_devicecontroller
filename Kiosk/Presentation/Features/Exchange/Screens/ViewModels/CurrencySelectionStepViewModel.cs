namespace Kiosk.ViewModels.Steps;

public sealed class CurrencySelectionStepViewModel : ExchangeStepViewModelBase
{
    public CurrencySelectionStepViewModel(
        IReadOnlyList<CurrencyOptionViewModel> options,
        string? title = "충전하실 통화를 선택해주세요",
        string? body = "충전하실 통화를 선택해주세요")
    {
        Title = title;
        Body = body;
        Options = options;
    }

    public IReadOnlyList<CurrencyOptionViewModel> Options { get; }
}
