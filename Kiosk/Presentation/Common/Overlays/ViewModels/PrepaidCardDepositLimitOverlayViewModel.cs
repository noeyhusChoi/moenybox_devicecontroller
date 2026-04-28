using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace Kiosk.ViewModels.Overlays;

public sealed class PrepaidCardDepositLimitOverlayViewModel
{
    public PrepaidCardDepositLimitOverlayViewModel(
        string sourceCurrencyCode,
        string dailyMaximumAmountText,
        string dailyRemainingAmountText,
        IRelayCommand closeCommand)
    {
        var normalizedCurrencyCode = string.IsNullOrWhiteSpace(sourceCurrencyCode)
            ? "USD"
            : sourceCurrencyCode.ToUpperInvariant();

        Title = "충전 금액 및 한도 금액";
        PrepaidWalletLimitText = FormatCurrency(500_000, "KRW");
        TrafficWalletLimitText = FormatCurrency(500_000, "KRW");
        DailyMaximumExchangeText = $"{dailyMaximumAmountText} {normalizedCurrencyCode}";
        DailyRemainingExchangeText = $"{dailyRemainingAmountText} {normalizedCurrencyCode}";
        ConfirmCommand = closeCommand;
    }

    public string Title { get; }

    public string PrepaidWalletLimitText { get; }

    public string TrafficWalletLimitText { get; }

    public string DailyMaximumExchangeText { get; }

    public string DailyRemainingExchangeText { get; }

    public IRelayCommand ConfirmCommand { get; }

    private static string FormatCurrency(int amount, string currencyCode)
        => string.Create(CultureInfo.InvariantCulture, $"{amount:N0} {currencyCode}");
}
