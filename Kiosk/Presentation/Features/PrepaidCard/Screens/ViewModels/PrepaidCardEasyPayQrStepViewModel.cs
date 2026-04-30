using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardEasyPayQrStepViewModel : ExchangeStepViewModelBase
{
    private const string CurrencyCode = "KRW";
    private const string QrSampleImageSource = "pack://application:,,,/Assets/Image/img_easy_pay_qr_sample.png";
    private static readonly Uri AlipayIconSource = new(
        "pack://application:,,,/KIOSK;component/Assets/Image/ico_ali.png",
        UriKind.Absolute);
    private static readonly Uri WechatPayIconSource = new(
        "pack://application:,,,/KIOSK;component/Assets/Image/ico_wechat.png",
        UriKind.Absolute);

    public PrepaidCardEasyPayQrStepViewModel(
        PrepaidCardEasyPayKind paymentKind,
        int chargeAmount)
    {
        PaymentMethodName = paymentKind == PrepaidCardEasyPayKind.Alipay ? "Alipay" : "WeChat Pay";
        PaymentMethodKoreanName = paymentKind == PrepaidCardEasyPayKind.Alipay ? "알리페이" : "위챗페이";
        PaymentIconSource = paymentKind == PrepaidCardEasyPayKind.Alipay ? AlipayIconSource : WechatPayIconSource;
        ChargeAmount = Math.Max(0, chargeAmount);
    }

    public string PaymentMethodName { get; }

    public string PaymentMethodKoreanName { get; }

    public string QrImageSource => QrSampleImageSource;

    public Uri PaymentIconSource { get; }

    public int ChargeAmount { get; }

    public string ChargeAmountNumberText => ChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string ChargeAmountCurrencyText => CurrencyCode;
}
