using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeDepositUseCase : IExchangeDepositUseCase
    {
        private readonly IExchangeDepositSessionService _sessionService;

        public ExchangeDepositUseCase(
            IExchangeDepositSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public event Action<bool>? DepositStateChanged;

        public async Task StartAsync(CancellationToken ct)
        {
            _sessionService.DepositStateChanged += OnDepositStateChanged;
            await _sessionService.StartAsync(ct);
        }

        public async Task StopAsync(CancellationToken ct)
        {
            _sessionService.DepositStateChanged -= OnDepositStateChanged;
            await _sessionService.StopAsync(ct);
        }

        private void OnDepositStateChanged(bool hasDeposit)
        {
            DepositStateChanged?.Invoke(hasDeposit);
        }
    }
}
