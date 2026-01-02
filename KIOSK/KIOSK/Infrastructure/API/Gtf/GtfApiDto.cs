using System.Text.Json.Serialization;

namespace KIOSK.Infrastructure.API.Gtf
{
    // 파일 예: API/Gtf/GtfDtos.cs
    #region /operation/initial

    public sealed class InitialRequestDto
    {
        [JsonPropertyName("edi")]
        public string? Edi { get; set; }

        [JsonPropertyName("tml_id")]
        public string? TmlId { get; set; }

        [JsonPropertyName("shop_name")]
        public string? ShopName { get; set; }
    }

    public sealed class InitialResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }

        [JsonPropertyName("kiosk_no")]
        public string? KioskNo { get; set; }

        [JsonPropertyName("kiosk_type")]
        public string? KioskType { get; set; }

        [JsonPropertyName("refund_limit_amt")]
        public string? RefundLimitAmt { get; set; }
    }

    #endregion

    #region /trc/inquirySlipList

    public sealed class InquirySlipListRequestDto
    {
        [JsonPropertyName("kiosk_no")]
        public string? KioskNo { get; set; }

        [JsonPropertyName("kiosk_type")]
        public string? KioskType { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("passport_no")]
        public string? PassportNo { get; set; }

        [JsonPropertyName("nationality_code")]
        public string? NationalityCode { get; set; }

        [JsonPropertyName("birthday")]
        public string? Birthday { get; set; }

        [JsonPropertyName("passport_expirdate")]
        public string? PassportExpirdate { get; set; }

        [JsonPropertyName("gender_code")]
        public string? GenderCode { get; set; }

        [JsonPropertyName("input_way_code")]
        public string? InputWayCode { get; set; } = "02"; // 고정값
    }

    public sealed class InquirySlipListResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }

        [JsonPropertyName("passport_serial_no")]
        public string? PassportSerialNo { get; set; }
    }

    #endregion

    #region /trc/registerSlip

    public sealed class RegisterSlipRequestDto
    {
        [JsonPropertyName("kiosk_no")]
        public string? KioskNo { get; set; }

        [JsonPropertyName("kiosk_type")]
        public string? KioskType { get; set; } = "01"; // 고정 값

        [JsonPropertyName("edi")]
        public string? Edi { get; set; }

        [JsonPropertyName("refund_type_code")]
        public string? RefundTypeCode { get; set; } = "02"; // 고정 값

        [JsonPropertyName("passport_no")]
        public string? PassportNo { get; set; }

        [JsonPropertyName("nationality_code")]
        public string? NationalityCode { get; set; }

        [JsonPropertyName("passport_serial_no")]
        public string? PassportSerialNo { get; set; }

        [JsonPropertyName("qr_data_type")]
        public string? QrDataType { get; set; } = "01"; // 고정 값

        [JsonPropertyName("qr_data")]
        public string? QrData { get; set; }
    }

    public sealed class RegisterSlipResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }

        [JsonPropertyName("passport_serial_no")]
        public string? PassportSerialNo { get; set; }

        [JsonPropertyName("rows")]
        public string? Rows { get; set; }

        // 전표 리스트
        [JsonPropertyName("slip_list")]
        public List<RegisterSlipItemDto> List { get; set; } = new();
    }

    public sealed class RegisterSlipItemDto
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

    #endregion

    #region /trc/possibility

    public sealed class PossibilityRequestDto
    {
        [JsonPropertyName("kiosk_no")]
        public string? KioskNo { get; set; }

        [JsonPropertyName("kiosk_type")]
        public string? KioskType { get; set; } = "1"; // 고정 값

        [JsonPropertyName("edi")]
        public string? Edi { get; set; }

        [JsonPropertyName("refund_type_code")]
        public string? RefundTypeCode { get; set; } = "02"; // 고정 값

        [JsonPropertyName("refund_no")]
        public string? RefundNo { get; set; }

        [JsonPropertyName("buy_serial_no")]
        public string[]? BuySerialNo { get; set; }

        [JsonPropertyName("number_of_slip")]
        public string? NumberOfSlip { get; set; }
    }

    public sealed class PossibilityResponseDto
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

    #endregion

    #region /trc/rollback

    public sealed class RollbackRequestDto
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

    public sealed class RollbackResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }
    }

    #endregion

    #region /refund/alipayConfirm

    public sealed class AlipayConfirmRequestDto
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

    public sealed class AlipayConfirmResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }

        [JsonPropertyName("list_no")]
        public string? ListNo { get; set; }

        [JsonPropertyName("list")]
        public List<AlipayUserInfo>? List { get; set; }
    }


    public sealed class AlipayUserInfo
    {
        [JsonPropertyName("alipay_user_name")]
        public string? AlipayUserName { get; set; }

        [JsonPropertyName("alipay_user_id")]
        public string? AlipayUserId { get; set; }

        [JsonPropertyName("alipay_login_id")]
        public string? AlipayLoginId { get; set; }
    }

    #endregion

    #region /refund/alipayRefund

    public sealed class AlipayRefundRequestDto
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

    public sealed class AlipayRefundResponseDto
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

    #endregion

    #region /refund/availability

    public sealed class AvailabilityRequestDto
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

    public sealed class AvailabilityResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }
    }

    #endregion

    #region /trc/customsResult

    public sealed class CustomsResultRequestDto
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

    public sealed class CustomsResultResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }
    }

    #endregion

    #region /trc/customsCancel

    public sealed class CustomsCancelRequestDto
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

    public sealed class CustomsCancelResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }
    }

    #endregion

    #region /refund/depositAmt

    public sealed class DepositAmtRequestDto
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

    public sealed class DepositAmtResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }

        [JsonPropertyName("deposit_amt")]
        public string? DepositAmt { get; set; }
    }

    #endregion

    #region /refund/cardRefund

    public sealed class CardRefundRequestDto
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

    public sealed class CardRefundResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }

        [JsonPropertyName("refund_no")]
        public string? RefundNo { get; set; }
    }

    #endregion

    #region /refund/saveMediSign

    public sealed class SaveMediSignRequestDto
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

    public sealed class SaveMediSignResponseDto
    {
        [JsonPropertyName("rc")]
        public string? Rc { get; set; }

        [JsonPropertyName("rm")]
        public string? Rm { get; set; }
    }

    #endregion

    #region /refund/wechatRefund

    public sealed class WechatRefundRequestDto
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

    public sealed class WechatRefundResponseDto
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

    #endregion
}
