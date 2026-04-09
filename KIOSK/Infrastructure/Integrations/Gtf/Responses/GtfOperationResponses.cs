using System.Text.Json.Serialization;

namespace Kiosk.Infrastructure.Integrations.Gtf.Responses;

public sealed class GtfRegisterSlipResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }

    [JsonPropertyName("rm")]
    public string? Rm { get; set; }

    [JsonPropertyName("passport_serial_no")]
    public string? PassportSerialNo { get; set; }

    [JsonPropertyName("rows")]
    public string? Rows { get; set; }

    [JsonPropertyName("slip_list")]
    public List<GtfRegisterSlipItem> List { get; set; } = new();
}

public sealed class GtfRegisterSlipItem
{
    [JsonPropertyName("buy_serial_no")]
    public string? BuySerialNo { get; set; }

    [JsonPropertyName("sell_date")]
    public string? SellDate { get; set; }

    [JsonPropertyName("sell_time")]
    public string? SellTime { get; set; }

    [JsonPropertyName("total_buy_amt")]
    public string? TotalBuyAmt { get; set; }

    [JsonPropertyName("total_refund_amt")]
    public string? TotalRefundAmt { get; set; }

    [JsonPropertyName("qty")]
    public string? Qty { get; set; }

    [JsonPropertyName("total_tax_amt")]
    public string? TotalTaxAmt { get; set; }

    [JsonPropertyName("slip_status_code")]
    public string? SlipStatusCode { get; set; }

    [JsonPropertyName("hotel_refund_yn")]
    public string? HotelRefundYn { get; set; }

    [JsonPropertyName("medi_refund_yn")]
    public string? MediRefundYn { get; set; }
}

public sealed class GtfPossibilityResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
    [JsonPropertyName("buy_serial_no")]
    public string[]? BuySerialNo { get; set; }
}

public sealed class GtfRollbackResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
}

public sealed class GtfAlipayConfirmResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
    [JsonPropertyName("list_no")]
    public string? ListNo { get; set; }
    [JsonPropertyName("list")]
    public List<GtfAlipayUser>? List { get; set; }
}

public sealed class GtfAlipayUser
{
    [JsonPropertyName("alipay_user_name")]
    public string? AlipayUserName { get; set; }
    [JsonPropertyName("alipay_user_id")]
    public string? AlipayUserId { get; set; }
    [JsonPropertyName("alipay_login_id")]
    public string? AlipayLoginId { get; set; }
}

public sealed class GtfAlipayRefundResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
    [JsonPropertyName("total_alipay_refund_amt")]
    public string? TotalAlipayRefundAmt { get; set; }
}

public sealed class GtfAvailabilityResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
}

public sealed class GtfDepositAmountResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
    [JsonPropertyName("deposit_amt")]
    public string? DepositAmt { get; set; }
}

public sealed class GtfCardRefundResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
}

public sealed class GtfSaveMediSignResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
}

public sealed class GtfWechatRefundResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
    [JsonPropertyName("refund_no")]
    public string? RefundNo { get; set; }
    [JsonPropertyName("total_wechat_refund_amt")]
    public string? TotalWechatRefundAmt { get; set; }
}

public sealed class GtfCustomsResultResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
}

public sealed class GtfCustomsCancelResponse
{
    [JsonPropertyName("rc")]
    public string? Rc { get; set; }
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }
}
