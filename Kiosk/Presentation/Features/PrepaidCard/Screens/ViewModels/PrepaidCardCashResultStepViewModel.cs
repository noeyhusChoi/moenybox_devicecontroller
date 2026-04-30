using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardCashResultStepViewModel : ExchangeStepViewModelBase
{
    private const string CurrencyCode = "KRW";

    public PrepaidCardCashResultStepViewModel(
        bool isSuccess,
        PrepaidCardWalletKind walletKind,
        int chargeAmount)
    {
        IsSuccess = isSuccess;
        WalletKind = walletKind;
        ChargeAmount = Math.Max(0, chargeAmount);
    }

    public bool IsSuccess { get; }

    public PrepaidCardWalletKind WalletKind { get; }

    public string WalletName => WalletKind == PrepaidCardWalletKind.Traffic
        ? "교통지갑"
        : "선불지갑";

    public string ResultTitle => IsSuccess ? "거래가 완료되었습니다" : "거래가 실패했습니다";

    public string ChargeSummaryLabel => $"{WalletName} 충전";

    public int ChargeAmount { get; }

    public string ChargeAmountNumberText => ChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string ChargeAmountCurrencyText => CurrencyCode;

    public int WalletBalanceAfterCharge => ChargeAmount;

    public string WalletBalanceAfterChargeNumberText => WalletBalanceAfterCharge.ToString("N0", CultureInfo.InvariantCulture);

    public string WalletBalanceAfterChargeCurrencyText => CurrencyCode;
}
