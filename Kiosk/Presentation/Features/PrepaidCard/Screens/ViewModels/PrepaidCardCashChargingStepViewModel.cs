using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardCashChargingStepViewModel : ExchangeStepViewModelBase
{
    private const string CurrencyCode = "KRW";

    public PrepaidCardCashChargingStepViewModel(
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

    public string ChargingTitle => $"{WalletName} 충전중입니다";

    public int ChargeAmount { get; }

    public string ChargeAmountNumberText => ChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string ChargeAmountCurrencyText => CurrencyCode;
}
