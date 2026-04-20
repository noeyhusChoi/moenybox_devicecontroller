using System.Globalization;

namespace Kiosk.ViewModels.Steps;

public sealed class DispenseFailureStepViewModel : ExchangeStepViewModelBase
{
    public DispenseFailureStepViewModel()
        : this(
            "USD",
            "KRW",
            100m,
            149000m,
            147000m,
            "출금 장치 상태를 확인할 수 없습니다.\n영수증 출력 후 관리자에게 문의해주세요.")
    {
    }

    public DispenseFailureStepViewModel(
        string sourceCurrencyCode,
        string targetCurrencyCode,
        decimal depositAmount,
        decimal requestedAmount,
        decimal dispensedAmount,
        string? errorMessage)
    {
        Title = string.Empty;

        SourceCurrencyCode = DispenseResultViewModelSupport.NormalizeCurrency(sourceCurrencyCode, "USD");
        TargetCurrencyCode = DispenseResultViewModelSupport.NormalizeCurrency(targetCurrencyCode, "KRW");
        SourceFlagImagePath = DispenseResultViewModelSupport.CreateFlagPath(SourceCurrencyCode);
        TargetFlagImagePath = DispenseResultViewModelSupport.CreateFlagPath(TargetCurrencyCode);
        Headline = "출금 중 문제가 발생하였습니다.";
        DepositAmountText = depositAmount.ToString("0.##", CultureInfo.InvariantCulture);
        DispensedAmountText = dispensedAmount.ToString("#,0.##", CultureInfo.InvariantCulture);
        UndispensedAmountText = Math.Max(0m, requestedAmount - dispensedAmount).ToString("#,0.##", CultureInfo.InvariantCulture);
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
            ? "불편을 드려 죄송합니다.\n영수증 출력 후 관리자에게 문의해주세요."
            : errorMessage;
    }

    public string SourceCurrencyCode { get; }
    public string TargetCurrencyCode { get; }
    public string? SourceFlagImagePath { get; }
    public string? TargetFlagImagePath { get; }
    public string Headline { get; }
    public string DepositAmountText { get; }
    public string DispensedAmountText { get; }
    public string UndispensedAmountText { get; }
    public string ErrorMessage { get; }
}
