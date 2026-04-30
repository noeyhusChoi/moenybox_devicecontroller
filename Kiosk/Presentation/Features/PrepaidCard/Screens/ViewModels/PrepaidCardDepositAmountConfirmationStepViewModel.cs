using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardDepositAmountConfirmationStepViewModel : ExchangeStepViewModelBase
{
    private const string CurrencyCode = "KRW";

    public PrepaidCardDepositAmountConfirmationStepViewModel(
        PrepaidCardWalletKind walletKind,
        int chargeAmount)
    {
        WalletKind = walletKind;
        ChargeAmount = Math.Max(0, chargeAmount);
    }

    public PrepaidCardWalletKind WalletKind { get; }

    public string WalletName => WalletKind == PrepaidCardWalletKind.Traffic
        ? "교통지갑"
        : "선불지갑";

    public string WalletChargeAmountLabel => $"{WalletName} 충전금액";

    public string ConfirmButtonText => $"{WalletName} 충전하기";

    public int ChargeAmount { get; }

    public string ChargeAmountNumberText => ChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string ChargeAmountCurrencyText => CurrencyCode;
}
