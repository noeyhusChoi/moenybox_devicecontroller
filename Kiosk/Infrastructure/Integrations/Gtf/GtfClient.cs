using System.Net.Http;
using System.Text;
using System.Text.Json;
using Kiosk.Infrastructure.Integrations.Common;
using Kiosk.Infrastructure.Integrations.Gtf.Models;
using Kiosk.Infrastructure.Integrations.Gtf.Requests;
using Kiosk.Infrastructure.Integrations.Gtf.Responses;

namespace Kiosk.Infrastructure.Integrations.Gtf;

public sealed class GtfClient : IGtfClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpExecutor _httpExecutor;
    private readonly IProviderConfigResolver _configResolver;

    public GtfClient(IHttpExecutor httpExecutor, IProviderConfigResolver configResolver)
    {
        _httpExecutor = httpExecutor;
        _configResolver = configResolver;
    }

    public Task<GtfApiResult<GtfInitialResponse>> InitialAsync(GtfInitialRequest request, CancellationToken ct = default)
        => SendAsync<GtfInitialRequest, GtfInitialResponse>("/operation/initial", request, ct);

    public Task<GtfApiResult<GtfInquirySlipListResponse>> InquirySlipListAsync(GtfInquirySlipListRequest request, CancellationToken ct = default)
        => SendAsync<GtfInquirySlipListRequest, GtfInquirySlipListResponse>("/trc/inquirySlipList", request, ct);

    public Task<GtfApiResult<GtfRegisterSlipResponse>> RegisterSlipAsync(GtfRegisterSlipRequest request, CancellationToken ct = default)
        => SendAsync<GtfRegisterSlipRequest, GtfRegisterSlipResponse>("/trc/registerSlip", request, ct);

    public Task<GtfApiResult<GtfPossibilityResponse>> PossibilityAsync(GtfPossibilityRequest request, CancellationToken ct = default)
        => SendAsync<GtfPossibilityRequest, GtfPossibilityResponse>("/trc/possibility", request, ct);

    public Task<GtfApiResult<GtfRollbackResponse>> RollbackAsync(GtfRollbackRequest request, CancellationToken ct = default)
        => SendAsync<GtfRollbackRequest, GtfRollbackResponse>("/trc/rollback", request, ct);

    public Task<GtfApiResult<GtfAlipayConfirmResponse>> AlipayConfirmAsync(GtfAlipayConfirmRequest request, CancellationToken ct = default)
        => SendAsync<GtfAlipayConfirmRequest, GtfAlipayConfirmResponse>("/refund/alipayConfirm", request, ct);

    public Task<GtfApiResult<GtfAlipayRefundResponse>> AlipayRefundAsync(GtfAlipayRefundRequest request, CancellationToken ct = default)
        => SendAsync<GtfAlipayRefundRequest, GtfAlipayRefundResponse>("/refund/alipayRefund", request, ct);

    public Task<GtfApiResult<GtfAvailabilityResponse>> AvailabilityAsync(GtfAvailabilityRequest request, CancellationToken ct = default)
        => SendAsync<GtfAvailabilityRequest, GtfAvailabilityResponse>("/refund/availability", request, ct);

    public Task<GtfApiResult<GtfDepositAmountResponse>> DepositAmountAsync(GtfDepositAmountRequest request, CancellationToken ct = default)
        => SendAsync<GtfDepositAmountRequest, GtfDepositAmountResponse>("/refund/depositAmt", request, ct);

    public Task<GtfApiResult<GtfCardRefundResponse>> CardRefundAsync(GtfCardRefundRequest request, CancellationToken ct = default)
        => SendAsync<GtfCardRefundRequest, GtfCardRefundResponse>("/refund/cardRefund", request, ct);

    public Task<GtfApiResult<GtfSaveMediSignResponse>> SaveMediSignAsync(GtfSaveMediSignRequest request, CancellationToken ct = default)
        => SendAsync<GtfSaveMediSignRequest, GtfSaveMediSignResponse>("/refund/saveMediSign", request, ct);

    public Task<GtfApiResult<GtfWechatRefundResponse>> WechatRefundAsync(GtfWechatRefundRequest request, CancellationToken ct = default)
        => SendAsync<GtfWechatRefundRequest, GtfWechatRefundResponse>("/refund/wechatRefund", request, ct);

    public Task<GtfApiResult<GtfCustomsResultResponse>> CustomsResultAsync(GtfCustomsResultRequest request, CancellationToken ct = default)
        => SendAsync<GtfCustomsResultRequest, GtfCustomsResultResponse>("/trc/customsResult", request, ct);

    public Task<GtfApiResult<GtfCustomsCancelResponse>> CustomsCancelAsync(GtfCustomsCancelRequest request, CancellationToken ct = default)
        => SendAsync<GtfCustomsCancelRequest, GtfCustomsCancelResponse>("/trc/customsCancel", request, ct);

    private async Task<GtfApiResult<TResponse>> SendAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
    {
        var config = _configResolver.GetRequired("GTF");
        var correlationId = Guid.NewGuid().ToString("N");
        var requestUri = $"{config.BaseUrl.TrimEnd('/')}{route}";
        var payload = JsonSerializer.Serialize(request, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var execution = await _httpExecutor.SendAsync(
            httpRequest,
            new HttpExecutionOptions(config.Timeout, config.RetryCount, correlationId),
            ct);

        if (!execution.Success)
        {
            return new GtfApiResult<TResponse>(
                false,
                default,
                new GtfError("HTTP_ERROR", execution.Exception?.Message ?? execution.RawBody, execution.StatusCode is >= 500),
                execution.StatusCode,
                execution.RawBody,
                execution.CorrelationId);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<TResponse>(execution.RawBody, JsonOptions);
            if (parsed is null)
            {
                return new GtfApiResult<TResponse>(
                    false,
                    default,
                    new GtfError("PARSE_ERROR", "Failed to deserialize GTF response.", false),
                    execution.StatusCode,
                    execution.RawBody,
                    execution.CorrelationId);
            }

            var (success, code, message) = InspectResponse(parsed);
            return new GtfApiResult<TResponse>(
                success,
                parsed,
                success ? null : new GtfError(code ?? "GTF_ERROR", message ?? "GTF rejected request.", false),
                execution.StatusCode,
                execution.RawBody,
                execution.CorrelationId);
        }
        catch (Exception ex)
        {
            return new GtfApiResult<TResponse>(
                false,
                default,
                new GtfError("PARSE_ERROR", ex.Message, false),
                execution.StatusCode,
                execution.RawBody,
                execution.CorrelationId);
        }
    }

    private static (bool success, string? code, string? message) InspectResponse<TResponse>(TResponse response)
    {
        var type = typeof(TResponse);
        var code = type.GetProperty("Rc")?.GetValue(response) as string;
        var message = type.GetProperty("Rm")?.GetValue(response) as string;
        var success = string.IsNullOrWhiteSpace(code) || code is "0000" or "00";
        return (success, code, message);
    }
}
