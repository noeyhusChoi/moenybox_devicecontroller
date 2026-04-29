using Kiosk.Application.Contracts;
using Kiosk.Application.Models.Common;
using Kiosk.Application.Models.TaxRefund;
using Kiosk.Infrastructure.Integrations.Gtf;
using Kiosk.Infrastructure.Integrations.Gtf.Models;
using Kiosk.Infrastructure.Integrations.Gtf.Requests;
using Kiosk.Infrastructure.Integrations.Gtf.Responses;

namespace Kiosk.Application.Services.TaxRefund;

public sealed class TaxRefundService : ITaxRefundService
{
    private readonly IGtfClient _gtfClient;

    public TaxRefundService(IGtfClient gtfClient)
    {
        _gtfClient = gtfClient;
    }

    public async Task<OperationResult<TaxRefundInitializationResult>> InitializeAsync(
        string edi,
        string terminalId,
        string shopName,
        CancellationToken ct = default)
    {
        var request = new GtfInitialRequest { Edi = edi, TmlId = terminalId, ShopName = shopName };
        var result = await _gtfClient.InitialAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundInitializationResult, GtfInitialResponse>(result, "Failed to initialize tax refund.");

        var appResult = new TaxRefundInitializationResult(
            result.Data.KioskNo,
            result.Data.KioskType,
            result.Data.RefundLimitAmt,
            "GTF",
            result.Data.Rm);

        return OperationResult<TaxRefundInitializationResult>.FromSuccess(appResult, "GTF", result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundCustomerLookupResult>> LookupCustomerAsync(
        TaxRefundCustomerLookupQuery query,
        CancellationToken ct = default)
    {
        var request = new GtfInquirySlipListRequest
        {
            KioskNo = query.KioskNo,
            KioskType = query.KioskType,
            Name = query.Name,
            PassportNo = query.PassportNo,
            NationalityCode = query.NationalityCode,
            Birthday = query.Birthday,
            PassportExpirdate = query.PassportExpireDate,
            GenderCode = query.GenderCode
        };

        var result = await _gtfClient.InquirySlipListAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundCustomerLookupResult, GtfInquirySlipListResponse>(result, "Failed to lookup tax refund customer.");

        return OperationResult<TaxRefundCustomerLookupResult>.FromSuccess(
            new TaxRefundCustomerLookupResult(result.Data.PassportSerialNo, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundSlipRegistrationResult>> RegisterSlipAsync(TaxRefundSlipRegistrationCommand command, CancellationToken ct = default)
    {
        var request = new GtfRegisterSlipRequest
        {
            KioskNo = command.KioskNo,
            KioskType = command.KioskType,
            Edi = command.Edi,
            RefundTypeCode = command.RefundTypeCode,
            PassportNo = command.PassportNo,
            NationalityCode = command.NationalityCode,
            PassportSerialNo = command.PassportSerialNo,
            QrDataType = command.QrDataType,
            QrData = command.QrData
        };

        var result = await _gtfClient.RegisterSlipAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundSlipRegistrationResult, GtfRegisterSlipResponse>(result, "Failed to register slip.");

        var totalRefund = result.Data.List.FirstOrDefault()?.TotalRefundAmt;
        return OperationResult<TaxRefundSlipRegistrationResult>.FromSuccess(
            new TaxRefundSlipRegistrationResult(result.Data.PassportSerialNo, result.Data.List.Count, totalRefund, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundEligibilityResult>> CheckEligibilityAsync(TaxRefundEligibilityQuery query, CancellationToken ct = default)
    {
        var request = new GtfPossibilityRequest
        {
            KioskNo = query.KioskNo,
            KioskType = query.KioskType,
            Edi = query.Edi,
            RefundTypeCode = query.RefundTypeCode,
            RefundNo = query.RefundNo,
            BuySerialNo = query.BuySerialNos.ToArray(),
            NumberOfSlip = query.NumberOfSlip
        };

        var result = await _gtfClient.PossibilityAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundEligibilityResult, GtfPossibilityResponse>(result, "Failed to check tax refund eligibility.");

        return OperationResult<TaxRefundEligibilityResult>.FromSuccess(
            new TaxRefundEligibilityResult(result.Data.RefundNo, result.Data.BuySerialNo ?? [], "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundRollbackResult>> RollbackAsync(TaxRefundRollbackCommand command, CancellationToken ct = default)
    {
        var request = new GtfRollbackRequest
        {
            KioskNo = command.KioskNo,
            KioskType = command.KioskType,
            Edi = command.Edi,
            RefundTypeCode = command.RefundTypeCode,
            RefundWayCode = command.RefundWayCode,
            RefundNo = command.RefundNo,
            BuySerialNo = command.BuySerialNos.ToArray(),
            NumberOfSlip = command.NumberOfSlip
        };

        var result = await _gtfClient.RollbackAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundRollbackResult, GtfRollbackResponse>(result, "Failed to rollback tax refund.");

        return OperationResult<TaxRefundRollbackResult>.FromSuccess(
            new TaxRefundRollbackResult(true, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundAlipayConfirmResult>> ConfirmAlipayAsync(TaxRefundAlipayConfirmQuery query, CancellationToken ct = default)
    {
        var request = new GtfAlipayConfirmRequest
        {
            KioskNo = query.KioskNo,
            KioskType = query.KioskType,
            Edi = query.Edi,
            RefundTypeCode = query.RefundTypeCode,
            RefundWayCode = query.RefundWayCode,
            AlipaySendType = query.AlipaySendType,
            AlipayId = query.AlipayId
        };

        var result = await _gtfClient.AlipayConfirmAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundAlipayConfirmResult, GtfAlipayConfirmResponse>(result, "Failed to confirm Alipay account.");

        var users = (result.Data.List ?? [])
            .Select(static x => new TaxRefundAlipayUser(x.AlipayUserName, x.AlipayUserId, x.AlipayLoginId))
            .ToList();

        return OperationResult<TaxRefundAlipayConfirmResult>.FromSuccess(
            new TaxRefundAlipayConfirmResult(result.Data.ListNo, users, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundPaymentResult>> RefundAlipayAsync(TaxRefundAlipayRefundCommand command, CancellationToken ct = default)
    {
        var request = new GtfAlipayRefundRequest
        {
            KioskNo = command.KioskNo,
            KioskType = command.KioskType,
            Edi = command.Edi,
            RefundTypeCode = command.RefundTypeCode,
            RefundWayCode = command.RefundWayCode,
            RefundNo = command.RefundNo,
            BuySerialNo = command.BuySerialNos.ToArray(),
            NumberOfSlip = command.NumberOfSlip,
            AlipaySendType = command.AlipaySendType,
            AlipayId = command.AlipayId
        };

        var result = await _gtfClient.AlipayRefundAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundPaymentResult, GtfAlipayRefundResponse>(result, "Failed to refund with Alipay.");

        return OperationResult<TaxRefundPaymentResult>.FromSuccess(
            new TaxRefundPaymentResult(result.Data.RefundNo, result.Data.TotalAlipayRefundAmt, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundAvailabilityResult>> CheckCardAvailabilityAsync(TaxRefundAvailabilityQuery query, CancellationToken ct = default)
    {
        var request = new GtfAvailabilityRequest
        {
            KioskNo = query.KioskNo,
            KioskType = query.KioskType,
            Edi = query.Edi,
            RefundNo = query.RefundNo,
            RefundTypeCode = query.RefundTypeCode,
            CardNo = query.CardNo
        };

        var result = await _gtfClient.AvailabilityAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundAvailabilityResult, GtfAvailabilityResponse>(result, "Failed to check card availability.");

        return OperationResult<TaxRefundAvailabilityResult>.FromSuccess(
            new TaxRefundAvailabilityResult(true, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundDepositAmountResult>> GetDepositAmountAsync(TaxRefundDepositAmountQuery query, CancellationToken ct = default)
    {
        var request = new GtfDepositAmountRequest
        {
            KioskNo = query.KioskNo,
            KioskType = query.KioskType,
            Edi = query.Edi,
            RefundTypeCode = query.RefundTypeCode,
            BuySerialNo = query.BuySerialNos.ToArray(),
            NumberOfSlip = query.NumberOfSlip
        };

        var result = await _gtfClient.DepositAmountAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundDepositAmountResult, GtfDepositAmountResponse>(result, "Failed to get deposit amount.");

        return OperationResult<TaxRefundDepositAmountResult>.FromSuccess(
            new TaxRefundDepositAmountResult(result.Data.DepositAmt, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundPaymentResult>> RefundCardAsync(TaxRefundCardRefundCommand command, CancellationToken ct = default)
    {
        var request = new GtfCardRefundRequest
        {
            KioskNo = command.KioskNo,
            KioskType = command.KioskType,
            Edi = command.Edi,
            RefundTypeCode = command.RefundTypeCode,
            RefundWayCode = command.RefundWayCode,
            RefundNo = command.RefundNo,
            BuySerialNo = command.BuySerialNos.ToArray(),
            NumberOfSlip = command.NumberOfSlip,
            CardNo = command.CardNo
        };

        var result = await _gtfClient.CardRefundAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundPaymentResult, GtfCardRefundResponse>(result, "Failed to refund to card.");

        return OperationResult<TaxRefundPaymentResult>.FromSuccess(
            new TaxRefundPaymentResult(result.Data.RefundNo, null, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundEvidenceUploadResult>> SaveMediSignAsync(TaxRefundSaveMediSignCommand command, CancellationToken ct = default)
    {
        var request = new GtfSaveMediSignRequest
        {
            KioskNo = command.KioskNo,
            KioskType = command.KioskType,
            Edi = command.Edi,
            RefundTypeCode = command.RefundTypeCode,
            RefundWayCode = command.RefundWayCode,
            BuySerialNo = command.BuySerialNos.ToArray(),
            NumberOfSlip = command.NumberOfSlip,
            SignImg = command.SignImage
        };

        var result = await _gtfClient.SaveMediSignAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundEvidenceUploadResult, GtfSaveMediSignResponse>(result, "Failed to save sign image.");

        return OperationResult<TaxRefundEvidenceUploadResult>.FromSuccess(
            new TaxRefundEvidenceUploadResult(true, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundPaymentResult>> RefundWechatAsync(TaxRefundWechatRefundCommand command, CancellationToken ct = default)
    {
        var request = new GtfWechatRefundRequest
        {
            KioskNo = command.KioskNo,
            KioskType = command.KioskType,
            Edi = command.Edi,
            RefundTypeCode = command.RefundTypeCode,
            RefundWayCode = command.RefundWayCode,
            RefundNo = command.RefundNo,
            BuySerialNo = command.BuySerialNos.ToArray(),
            NumberOfSlip = command.NumberOfSlip,
            WechatMiniBarcode = command.WechatMiniBarcode
        };

        var result = await _gtfClient.WechatRefundAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundPaymentResult, GtfWechatRefundResponse>(result, "Failed to refund with WeChat.");

        return OperationResult<TaxRefundPaymentResult>.FromSuccess(
            new TaxRefundPaymentResult(result.Data.RefundNo, result.Data.TotalWechatRefundAmt, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundCustomsOperationResult>> SendCustomsResultAsync(TaxRefundCustomsResultCommand command, CancellationToken ct = default)
    {
        var request = new GtfCustomsResultRequest
        {
            KioskNo = command.KioskNo,
            KioskType = command.KioskType,
            Edi = command.Edi,
            BuySerialNo = command.BuySerialNos.ToArray(),
            NumberOfSlip = command.NumberOfSlip
        };

        var result = await _gtfClient.CustomsResultAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundCustomsOperationResult, GtfCustomsResultResponse>(result, "Failed to send customs result.");

        return OperationResult<TaxRefundCustomsOperationResult>.FromSuccess(
            new TaxRefundCustomsOperationResult(true, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    public async Task<OperationResult<TaxRefundCustomsOperationResult>> CancelCustomsResultAsync(TaxRefundCustomsCancelCommand command, CancellationToken ct = default)
    {
        var request = new GtfCustomsCancelRequest
        {
            KioskNo = command.KioskNo,
            KioskType = command.KioskType,
            Edi = command.Edi,
            BuySerialNo = command.BuySerialNos.ToArray(),
            NumberOfSlip = command.NumberOfSlip
        };

        var result = await _gtfClient.CustomsCancelAsync(request, ct);
        if (!result.Success || result.Data is null)
            return Error<TaxRefundCustomsOperationResult, GtfCustomsCancelResponse>(result, "Failed to cancel customs result.");

        return OperationResult<TaxRefundCustomsOperationResult>.FromSuccess(
            new TaxRefundCustomsOperationResult(true, "GTF", result.Data.Rm),
            "GTF",
            result.CorrelationId);
    }

    private static OperationResult<T> Error<T, TData>(GtfApiResult<TData> result, string fallbackMessage)
    {
        var error = result.Error is null
            ? new AppError("UNKNOWN", fallbackMessage, "Unknown", false, "GTF")
            : new AppError(result.Error.Code, result.Error.Message, "ProviderRejected", result.Error.Retryable, "GTF");

        return OperationResult<T>.FromError(error, "GTF", result.CorrelationId);
    }
}
