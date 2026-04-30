using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardEasyPayChargingStepViewModel : ExchangeStepViewModelBase
{
    private const string CurrencyCode = "KRW";

    public PrepaidCardEasyPayChargingStepViewModel(int chargeAmount)
    {
        ChargeAmount = Math.Max(0, chargeAmount);
    }

    public int ChargeAmount { get; }

    public string ChargeAmountNumberText => ChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string ChargeAmountCurrencyText => CurrencyCode;
}
