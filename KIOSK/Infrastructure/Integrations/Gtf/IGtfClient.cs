using Kiosk.Infrastructure.Integrations.Gtf.Models;
using Kiosk.Infrastructure.Integrations.Gtf.Requests;
using Kiosk.Infrastructure.Integrations.Gtf.Responses;

namespace Kiosk.Infrastructure.Integrations.Gtf;

public interface IGtfClient
{
    Task<GtfApiResult<GtfInitialResponse>> InitialAsync(GtfInitialRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfInquirySlipListResponse>> InquirySlipListAsync(GtfInquirySlipListRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfRegisterSlipResponse>> RegisterSlipAsync(GtfRegisterSlipRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfPossibilityResponse>> PossibilityAsync(GtfPossibilityRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfRollbackResponse>> RollbackAsync(GtfRollbackRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfAlipayConfirmResponse>> AlipayConfirmAsync(GtfAlipayConfirmRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfAlipayRefundResponse>> AlipayRefundAsync(GtfAlipayRefundRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfAvailabilityResponse>> AvailabilityAsync(GtfAvailabilityRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfDepositAmountResponse>> DepositAmountAsync(GtfDepositAmountRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfCardRefundResponse>> CardRefundAsync(GtfCardRefundRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfSaveMediSignResponse>> SaveMediSignAsync(GtfSaveMediSignRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfWechatRefundResponse>> WechatRefundAsync(GtfWechatRefundRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfCustomsResultResponse>> CustomsResultAsync(GtfCustomsResultRequest request, CancellationToken ct = default);
    Task<GtfApiResult<GtfCustomsCancelResponse>> CustomsCancelAsync(GtfCustomsCancelRequest request, CancellationToken ct = default);
}
