using Kiosk.Application.Models.Common;
using Kiosk.Application.Models.TaxRefund;

namespace Kiosk.Application.Contracts;

public interface ITaxRefundService
{
    Task<OperationResult<TaxRefundInitializationResult>> InitializeAsync(
        string edi,
        string terminalId,
        string shopName,
        CancellationToken ct = default);

    Task<OperationResult<TaxRefundCustomerLookupResult>> LookupCustomerAsync(
        TaxRefundCustomerLookupQuery query,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundSlipRegistrationResult>> RegisterSlipAsync(
        TaxRefundSlipRegistrationCommand command,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundEligibilityResult>> CheckEligibilityAsync(
        TaxRefundEligibilityQuery query,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundRollbackResult>> RollbackAsync(
        TaxRefundRollbackCommand command,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundAlipayConfirmResult>> ConfirmAlipayAsync(
        TaxRefundAlipayConfirmQuery query,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundPaymentResult>> RefundAlipayAsync(
        TaxRefundAlipayRefundCommand command,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundAvailabilityResult>> CheckCardAvailabilityAsync(
        TaxRefundAvailabilityQuery query,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundDepositAmountResult>> GetDepositAmountAsync(
        TaxRefundDepositAmountQuery query,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundPaymentResult>> RefundCardAsync(
        TaxRefundCardRefundCommand command,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundEvidenceUploadResult>> SaveMediSignAsync(
        TaxRefundSaveMediSignCommand command,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundPaymentResult>> RefundWechatAsync(
        TaxRefundWechatRefundCommand command,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundCustomsOperationResult>> SendCustomsResultAsync(
        TaxRefundCustomsResultCommand command,
        CancellationToken ct = default);
    Task<OperationResult<TaxRefundCustomsOperationResult>> CancelCustomsResultAsync(
        TaxRefundCustomsCancelCommand command,
        CancellationToken ct = default);
}
