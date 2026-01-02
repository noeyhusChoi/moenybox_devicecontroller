using KIOSK.Infrastructure.API.Cems;
using KIOSK.Infrastructure.API.Core;
using KIOSK.Infrastructure.API.Gtf;

namespace KIOSK.Services.API
{
    public sealed class GtfApiService
    {
        private readonly IGtfApiCmdBuilder _builder;
        private readonly IApiClient _client;

        public GtfApiService(
            IGtfApiCmdBuilder builder,
            IApiClient client)
        {
            _builder = builder;
            _client = client;
        }

        public Task<InitialResponseDto> InitialAsync(InitialRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<InitialResponseDto>(_builder.Initial(req), ct);

        public Task<InquirySlipListResponseDto> InquirySlipListAsync(InquirySlipListRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<InquirySlipListResponseDto>(_builder.InquirySlipList(req), ct);

        public Task<RegisterSlipResponseDto> RegisterSlipAsync(RegisterSlipRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<RegisterSlipResponseDto>(_builder.RegisterSlip(req), ct);

        public Task<PossibilityResponseDto> PossibilityAsync(PossibilityRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<PossibilityResponseDto>(_builder.Possibility(req), ct);

        public Task<RollbackResponseDto> RollbackAsync(RollbackRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<RollbackResponseDto>(_builder.Rollback(req), ct);

        public Task<AlipayConfirmResponseDto> AlipayConfirmAsync(AlipayConfirmRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<AlipayConfirmResponseDto>(_builder.AlipayConfirm(req), ct);

        public Task<AlipayRefundResponseDto> AlipayRefundAsync(AlipayRefundRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<AlipayRefundResponseDto>(_builder.AlipayRefund(req), ct);

        public Task<AvailabilityResponseDto> AvailabilityAsync(AvailabilityRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<AvailabilityResponseDto>(_builder.Availability(req), ct);

        public Task<DepositAmtResponseDto> DepositAmtAsync(DepositAmtRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<DepositAmtResponseDto>(_builder.DepositAmt(req), ct);

        public Task<CardRefundResponseDto> CardRefundAsync(CardRefundRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<CardRefundResponseDto>(_builder.CardRefund(req), ct);

        public Task<SaveMediSignResponseDto> SaveMediSignAsync(SaveMediSignRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<SaveMediSignResponseDto>(_builder.SaveMediSign(req), ct);

        public Task<WechatRefundResponseDto> WechatRefundAsync(WechatRefundRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<WechatRefundResponseDto>(_builder.WechatRefund(req), ct);

        public Task<CustomsResultResponseDto> CustomsResultAsync(CustomsResultRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<CustomsResultResponseDto>(_builder.CustomsResult(req), ct);

        public Task<CustomsCancelResponseDto> CustomsCancelAsync(CustomsCancelRequestDto req, CancellationToken ct)
            => SendWithRetryAsync<CustomsCancelResponseDto>(_builder.CustomsCancel(req), ct);

        private async Task<T> SendWithRetryAsync<T>(ApiEnvelope env, CancellationToken ct)
        {
            var (parsed, raw) = await _client.SendWithRetryAsync(env, GtfApiResponse.Parse<T>, maxRetry: 1, ct);
            return parsed;
        }

        private async Task<T> SendOnceAsync<T>(ApiEnvelope env, CancellationToken ct)
        {
            var (parsed, raw) = await _client.SendOnceAsync(env, GtfApiResponse.Parse<T>, ct);
            return parsed;
        }
    }
}
