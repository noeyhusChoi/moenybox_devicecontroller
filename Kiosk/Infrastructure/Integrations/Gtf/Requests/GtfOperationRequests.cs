using System.Text.Json.Serialization;

namespace Kiosk.Infrastructure.Integrations.Gtf.Requests;

public sealed class GtfRegisterSlipRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }

    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; } = "01";

    [JsonPropertyName("edi")]
    public string? Edi { get; set; }

    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; } = "02";

    [JsonPropertyName("passport_no")]
    public string? PassportNo { get; set; }

    [JsonPropertyName("nationality_code")]
    public string? NationalityCode { get; set; }

    [JsonPropertyName("passport_serial_no")]
    public string? PassportSerialNo { get; set; }

    [JsonPropertyName("qr_data_type")]
    public string? QrDataType { get; set; } = "01";

    [JsonPropertyName("qr_data")]
    public string? QrData { get; set; }
}

public sealed class GtfPossibilityRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; } = "1";
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; } = "02";
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
    [JsonPropertyName("number_of_slip")]
    public string? NumberOfSlip { get; set; }
}

public sealed class GtfRollbackRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; }
    [JsonPropertyName("refund_way_code")]
    public string? RefundWayCode { get; set; }
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
    [JsonPropertyName("number_of_slip")]
    public string? NumberOfSlip { get; set; }
}

public sealed class GtfAlipayConfirmRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; }
    [JsonPropertyName("refund_way_code")]
    public string? RefundWayCode { get; set; }
    [JsonPropertyName("alipay_send_type")]
    public string? AlipaySendType { get; set; }
    [JsonPropertyName("alipay_id")]
    public string? AlipayId { get; set; }
}

public sealed class GtfAlipayRefundRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; }
    [JsonPropertyName("refund_way_code")]
    public string? RefundWayCode { get; set; }
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
    [JsonPropertyName("number_of_slip")]
    public string? NumberOfSlip { get; set; }
    [JsonPropertyName("alipay_send_type")]
    public string? AlipaySendType { get; set; }
    [JsonPropertyName("alipay_id")]
    public string? AlipayId { get; set; }
}

public sealed class GtfAvailabilityRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; }
    [JsonPropertyName("card_no")]
    public string? CardNo { get; set; }
}

public sealed class GtfDepositAmountRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; } = "02";
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
    [JsonPropertyName("number_of_slip")]
    public string? NumberOfSlip { get; set; }
}

public sealed class GtfCardRefundRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; }
    [JsonPropertyName("refund_way_code")]
    public string? RefundWayCode { get; set; }
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
    [JsonPropertyName("number_of_slip")]
    public string? NumberOfSlip { get; set; }
    [JsonPropertyName("card_no")]
    public string? CardNo { get; set; }
}

public sealed class GtfSaveMediSignRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; }
    [JsonPropertyName("refund_way_code")]
    public string? RefundWayCode { get; set; }
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
    [JsonPropertyName("number_of_slip")]
    public string? NumberOfSlip { get; set; }
    [JsonPropertyName("sign_img")]
    public string? SignImg { get; set; }
}

public sealed class GtfWechatRefundRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("refund_type_code")]
    public string? RefundTypeCode { get; set; }
    [JsonPropertyName("refund_way_code")]
    public string? RefundWayCode { get; set; }
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
    [JsonPropertyName("number_of_slip")]
    public string? NumberOfSlip { get; set; }
    [JsonPropertyName("wechat_mini_barcode")]
    public string? WechatMiniBarcode { get; set; }
}

public sealed class GtfCustomsResultRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
    [JsonPropertyName("number_of_slip")]
    public string? NumberOfSlip { get; set; }
}

public sealed class GtfCustomsCancelRequest
{
    [JsonPropertyName("kiosk_no")]
    public string? KioskNo { get; set; }
    [JsonPropertyName("kiosk_type")]
    public string? KioskType { get; set; }
    [JsonPropertyName("edi")]
    public string? Edi { get; set; }
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
    [JsonPropertyName("number_of_slip")]
    public string? NumberOfSlip { get; set; }
}
