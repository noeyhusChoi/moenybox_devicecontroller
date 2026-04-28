namespace Kiosk.Application.Models.TaxRefund;

public sealed record TaxRefundCustomerLookupQuery(
    string KioskNo,
    string KioskType,
    string Name,
    string PassportNo,
    string NationalityCode,
    string Birthday,
    string PassportExpireDate,
    string GenderCode);

public sealed record TaxRefundSlipRegistrationCommand(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundTypeCode,
    string PassportNo,
    string NationalityCode,
    string PassportSerialNo,
    string QrDataType,
    string QrData);

public sealed record TaxRefundSlipRegistrationResult(
    string? PassportSerialNo,
    int SlipCount,
    string? TotalRefundAmount,
    string Provider,
    string? Message);

public sealed record TaxRefundEligibilityQuery(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundTypeCode,
    string RefundNo,
    IReadOnlyList<string> BuySerialNos,
    string NumberOfSlip);

public sealed record TaxRefundEligibilityResult(
    string? RefundNo,
    IReadOnlyList<string> BuySerialNos,
    string Provider,
    string? Message);

public sealed record TaxRefundRollbackCommand(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundTypeCode,
    string RefundWayCode,
    string RefundNo,
    IReadOnlyList<string> BuySerialNos,
    string NumberOfSlip);

public sealed record TaxRefundRollbackResult(
    bool RolledBack,
    string Provider,
    string? Message);

public sealed record TaxRefundAlipayConfirmQuery(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundTypeCode,
    string RefundWayCode,
    string AlipaySendType,
    string AlipayId);

public sealed record TaxRefundAlipayUser(
    string? UserName,
    string? UserId,
    string? LoginId);

public sealed record TaxRefundAlipayConfirmResult(
    string? ListNo,
    IReadOnlyList<TaxRefundAlipayUser> Users,
    string Provider,
    string? Message);

public sealed record TaxRefundAlipayRefundCommand(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundTypeCode,
    string RefundWayCode,
    string RefundNo,
    IReadOnlyList<string> BuySerialNos,
    string NumberOfSlip,
    string AlipaySendType,
    string AlipayId);

public sealed record TaxRefundPaymentResult(
    string? RefundNo,
    string? Amount,
    string Provider,
    string? Message);

public sealed record TaxRefundAvailabilityQuery(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundNo,
    string RefundTypeCode,
    string CardNo);

public sealed record TaxRefundAvailabilityResult(
    bool Available,
    string Provider,
    string? Message);

public sealed record TaxRefundDepositAmountQuery(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundTypeCode,
    IReadOnlyList<string> BuySerialNos,
    string NumberOfSlip);

public sealed record TaxRefundDepositAmountResult(
    string? DepositAmount,
    string Provider,
    string? Message);

public sealed record TaxRefundCardRefundCommand(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundTypeCode,
    string RefundWayCode,
    string RefundNo,
    IReadOnlyList<string> BuySerialNos,
    string NumberOfSlip,
    string CardNo);

public sealed record TaxRefundSaveMediSignCommand(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundTypeCode,
    string RefundWayCode,
    IReadOnlyList<string> BuySerialNos,
    string NumberOfSlip,
    string SignImage);

public sealed record TaxRefundEvidenceUploadResult(
    bool Saved,
    string Provider,
    string? Message);

public sealed record TaxRefundWechatRefundCommand(
    string KioskNo,
    string KioskType,
    string Edi,
    string RefundTypeCode,
    string RefundWayCode,
    string RefundNo,
    IReadOnlyList<string> BuySerialNos,
    string NumberOfSlip,
    string WechatMiniBarcode);

public sealed record TaxRefundCustomsResultCommand(
    string KioskNo,
    string KioskType,
    string Edi,
    IReadOnlyList<string> BuySerialNos,
    string NumberOfSlip);

public sealed record TaxRefundCustomsCancelCommand(
    string KioskNo,
    string KioskType,
    string Edi,
    IReadOnlyList<string> BuySerialNos,
    string NumberOfSlip);

public sealed record TaxRefundCustomsOperationResult(
    bool Succeeded,
    string Provider,
    string? Message);
