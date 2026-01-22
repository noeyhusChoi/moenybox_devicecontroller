using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Domain.Entities;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeRateListUseCase : IExchangeRateListUseCase
    {
        private readonly IExchangeRateProvider _rateProvider;

        public ExchangeRateListUseCase(IExchangeRateProvider rateProvider)
        {
            _rateProvider = rateProvider;
        }

        public Task<IReadOnlyList<ExchangeRate>> LoadAsync(CancellationToken ct = default)
        {
            return _rateProvider.GetRatesAsync(ct);
        }
    }
}
