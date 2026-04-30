using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardEasyPayAmountConfirmationStepViewModel : ExchangeStepViewModelBase
{
    private const string CurrencyCode = "KRW";

    public PrepaidCardEasyPayAmountConfirmationStepViewModel(
        PrepaidCardEasyPayKind paymentKind,
        int chargeAmount)
    {
        PaymentMethodName = paymentKind == PrepaidCardEasyPayKind.Alipay ? "알리페이" : "위챗페이";
        ChargeAmount = Math.Max(0, chargeAmount);
    }

    public string PaymentMethodName { get; }

    public int ChargeAmount { get; }

    public string ChargeAmountNumberText => ChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string ChargeAmountCurrencyText => CurrencyCode;

    public string TotalPaymentAmountNumberText => ChargeAmountNumberText;

    public string TotalPaymentAmountCurrencyText => CurrencyCode;
}
