using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Domain.Entities;
using KIOSK.Application.Services;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeSelectCurrencyUseCase : IExchangeSelectCurrencyUseCase
    {
        private readonly ITransactionServiceV2 _transactionService;
        private readonly IExchangePolicyProvider _policyProvider;

        public ExchangeSelectCurrencyUseCase(
            ITransactionServiceV2 transactionService,
            IExchangePolicyProvider policyProvider)
        {
            _transactionService = transactionService;
            _policyProvider = policyProvider;
        }

        public async Task SelectAsync(ExchangeRate rate, CancellationToken ct = default)
        {
            if (rate == null)
                throw new ArgumentNullException(nameof(rate));
            if (string.IsNullOrWhiteSpace(rate.Currency))
                throw new ArgumentException("Currency is required.", nameof(rate));
            if (rate.SpSell is null)
                throw new ArgumentException("Exchange rate is required.", nameof(rate));

            await _transactionService.UpsertRateAsync(new CurrencyPair(rate.Currency, rate.SpSell.Value), ct);
            var policy = _policyProvider.GetPolicy(rate.Currency, "KRW", rate);
            await _transactionService.UpsertPolicyAsync(rate.Currency, "KRW", policy, ct);

            await _transactionService.NewAsync(rate.Currency, "KRW", ct);
        }
    }
}
