using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeWithdrawalUseCase : IExchangeWithdrawalUseCase
    {
        private readonly IExchangeDispenseSessionService _sessionService;

        public ExchangeWithdrawalUseCase(
            IExchangeDispenseSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            await _sessionService.ExecuteAsync(ct);
        }
    }
}
