using KIOSK.Infrastructure.API.Cems;
using KIOSK.Infrastructure.API.Core;
using KIOSK.Models;

namespace KIOSK.Services.API
{
    public sealed class CemsApiService
    {
        private readonly ICemsApiCmdBuilder _builder;
        private readonly IApiClient _client;

        public CemsApiService(ICemsApiCmdBuilder builder, IApiClient client)
        {
            _builder = builder;
            _client = client;
        }

        public Task<CemsApiResponse> CheckLimitAsync(string number, CancellationToken ct)
            => SendWithRetryAsync(_builder.C020(number), ct);

        public Task<CemsApiResponse> GetRateAsync(string currency, CancellationToken ct)
            => SendWithRetryAsync(_builder.C010(currency), ct);

        public Task<CemsApiResponse> GetRateAllAsync(CancellationToken ct)
           => SendWithRetryAsync(_builder.C011(), ct);

        public Task<CemsApiResponse> RegisterTransactionAsync(TransactionModelV2 s, CancellationToken ct)
            => SendWithRetryAsync(_builder.C030(s), ct);

        public Task<CemsApiResponse> SetCashAsync(IReadOnlySet<WithdrawalCassette> cash, CancellationToken ct)
            => SendWithRetryAsync(_builder.C040(cash), ct);

        public Task<CemsApiResponse> PullCashAsync(CancellationToken ct)
            => SendWithRetryAsync(_builder.C070(), ct);

        public Task<CemsApiResponse> ReportErrorAsync(DateTime dtKst, string msg, CancellationToken ct)
            => SendOnceAsync(_builder.C060(dtKst, msg), ct);

        public Task<CemsApiResponse> SmsAsync(DateTime dtKst, string type, string msg, CancellationToken ct)
            => SendOnceAsync(_builder.C090(dtKst, type, msg), ct);

        private async Task<CemsApiResponse> SendWithRetryAsync(ApiEnvelope env, CancellationToken ct)
        {
            var (parsed, raw) = await _client.SendWithRetryAsync(env, CemsApiResponse.Parse, maxRetry: 1, ct);
            return parsed;
        }

        private async Task<CemsApiResponse> SendOnceAsync(ApiEnvelope env, CancellationToken ct)
        {
            var (parsed, raw) = await _client.SendOnceAsync(env, CemsApiResponse.Parse, ct);
            return parsed;
        }
    }
}
