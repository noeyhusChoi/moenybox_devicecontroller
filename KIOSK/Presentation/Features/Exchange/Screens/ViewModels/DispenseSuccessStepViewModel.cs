using System.Globalization;
using Kiosk.Application.Services.Devices.Withdrawal;
using Kiosk.Application.Services.Exchange;

namespace Kiosk.ViewModels.Steps;

public sealed class DispenseSuccessStepViewModel : ExchangeStepViewModelBase
{
    public DispenseSuccessStepViewModel()
        : this(
            "USD",
            "KRW",
            1470m,
            100m,
            149000m,
            147000m,
            [
                new WithdrawalAllocation("HCDM1", 1, "KRW", 50000m, 2),
                new WithdrawalAllocation("HCDM1", 2, "KRW", 10000m, 4),
                new WithdrawalAllocation("HCDM1", 3, "KRW", 5000m, 1),
                new WithdrawalAllocation("HCDM1", 4, "KRW", 1000m, 2)
            ],
            [
                new CashBalanceSlot("HCDM1", 1, "KRW", 50000m, 100),
                new CashBalanceSlot("HCDM1", 2, "KRW", 10000m, 100),
                new CashBalanceSlot("HCDM1", 3, "KRW", 5000m, 100),
                new CashBalanceSlot("HCDM1", 4, "KRW", 1000m, 100)
            ])
    {
    }

    public DispenseSuccessStepViewModel(
        string sourceCurrencyCode,
        string targetCurrencyCode,
        decimal exchangeRate,
        decimal depositAmount,
        decimal requestedAmount,
        decimal dispensedAmount,
        IReadOnlyList<WithdrawalAllocation> allocations,
        IReadOnlyList<CashBalanceSlot> balanceSlots)
    {
        Title = string.Empty;

        SourceCurrencyCode = DispenseResultViewModelSupport.NormalizeCurrency(sourceCurrencyCode, "USD");
        TargetCurrencyCode = DispenseResultViewModelSupport.NormalizeCurrency(targetCurrencyCode, "KRW");
        SourceFlagImagePath = DispenseResultViewModelSupport.CreateFlagPath(SourceCurrencyCode);
        TargetFlagImagePath = DispenseResultViewModelSupport.CreateFlagPath(TargetCurrencyCode);

        Headline = "거래가 완료되었습니다";
        ExchangeRateText = exchangeRate.ToString("0.00", CultureInfo.InvariantCulture);
        DepositAmountText = depositAmount.ToString("0.##", CultureInfo.InvariantCulture);
        RemainingAmountText = Math.Max(0m, requestedAmount - dispensedAmount).ToString("#,0.##", CultureInfo.InvariantCulture);
        DispensedAmountText = dispensedAmount.ToString("#,0.##", CultureInfo.InvariantCulture);
        BreakdownRows = DispenseResultViewModelSupport.BuildBreakdownRows(TargetCurrencyCode, allocations, balanceSlots);
    }

    public string Headline { get; }
    public string SourceCurrencyCode { get; }
    public string TargetCurrencyCode { get; }
    public string? SourceFlagImagePath { get; }
    public string? TargetFlagImagePath { get; }
    public string ExchangeRateText { get; }
    public string DepositAmountText { get; }
    public string RemainingAmountText { get; }
    public bool HasRemainingAmount => RemainingAmountText != "0";
    public string DispensedAmountText { get; }
    public IReadOnlyList<DispenseBreakdownRowViewModel> BreakdownRows { get; }
}
