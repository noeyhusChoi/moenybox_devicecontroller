using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Domain.Entities;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeRateProvider : IExchangeRateProvider
    {
        private static readonly string[] ExcludedCurrencies = { "RUB" };
        private readonly ExchangeRateModel _exchangeRateModel;

        public ExchangeRateProvider(ExchangeRateModel exchangeRateModel)
        {
            _exchangeRateModel = exchangeRateModel;
        }

        public Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(CancellationToken ct = default)
        {
            var list = _exchangeRateModel.Data
                .Where(er => !ExcludedCurrencies.Contains(er.Currency, StringComparer.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult<IReadOnlyList<ExchangeRate>>(list);
        }
    }
}
