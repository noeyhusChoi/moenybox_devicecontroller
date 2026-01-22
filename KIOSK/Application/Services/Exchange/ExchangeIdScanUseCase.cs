using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeIdScanUseCase : IExchangeIdScanUseCase
    {
        private readonly IExchangeIdScanSessionService _sessionService;

        public ExchangeIdScanUseCase(
            IExchangeIdScanSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task<bool> ScanAsync(CancellationToken ct)
        {
            return await _sessionService.ScanAsync(ct);
        }
    }
}
