using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardEasyPayResultStepViewModel : ExchangeStepViewModelBase
{
    private const string CurrencyCode = "KRW";

    public PrepaidCardEasyPayResultStepViewModel(
        bool isSuccess,
        PrepaidCardEasyPayKind paymentKind,
        int chargeAmount)
    {
        IsSuccess = isSuccess;
        PaymentMethodName = paymentKind == PrepaidCardEasyPayKind.Alipay ? "알리페이" : "위챗페이";
        ChargeAmount = Math.Max(0, chargeAmount);
    }

    public bool IsSuccess { get; }

    public string ResultTitle => IsSuccess ? "거래가 완료되었습니다" : "거래가 실패했습니다";

    public string ResultMessage => IsSuccess
        ? "교통지갑 충전이 정상적으로 완료되었습니다."
        : "결제 또는 카드 충전 중 문제가 발생했습니다. 다시 시도하거나 고객센터에 문의해 주세요.";

    public string PaymentMethodName { get; }

    public int ChargeAmount { get; }

    public string ChargeAmountNumberText => ChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string ChargeAmountCurrencyText => CurrencyCode;

    public int TrafficWalletBalanceAfterCharge => ChargeAmount;

    public string TrafficWalletBalanceAfterChargeNumberText => TrafficWalletBalanceAfterCharge.ToString("N0", CultureInfo.InvariantCulture);

    public string TrafficWalletBalanceAfterChargeCurrencyText => CurrencyCode;
}
