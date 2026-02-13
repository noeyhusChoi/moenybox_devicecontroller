using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Domain.Transactions;
using DomainExchangePolicyInfo = KIOSK.Domain.Transactions.ExchangePolicyInfo;
using DomainExchangeRateInfo = KIOSK.Domain.Transactions.ExchangeRateInfo;

namespace KIOSK.Application.Features.ExchangeV2.UseCases
{
    public interface IExchangeV2PolicyUseCase
    {
        void SelectTransactionType(ExchangeTransactionType type);
        void SelectPayoutMethod(PayoutMethodType method);
        Task ApplyCurrencyAsync(string targetCurrency, decimal rate, CancellationToken ct = default);
    }

    public sealed class ExchangeV2PolicyUseCase : IExchangeV2PolicyUseCase
    {
        private readonly IExchangeV2TransactionContext _tx;
        private readonly IExchangeV2FlowPolicyResolver _resolver;
        private ExchangeTransactionType _selectedType = ExchangeTransactionType.Sell;
        private PayoutMethodType _selectedPayoutMethod = PayoutMethodType.Cash;

        public ExchangeV2PolicyUseCase(
            IExchangeV2TransactionContext tx,
            IExchangeV2FlowPolicyResolver resolver)
        {
            _tx = tx;
            _resolver = resolver;
        }

        public void SelectTransactionType(ExchangeTransactionType type)
        {
            _selectedType = type;
            _tx.SelectTransactionType(type);
        }

        public void SelectPayoutMethod(PayoutMethodType method)
        {
            _selectedPayoutMethod = method;
            _tx.SelectPayoutMethod(method);
        }

        public Task ApplyCurrencyAsync(string targetCurrency, decimal rate, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(targetCurrency))
                throw new ArgumentException("Target currency is required.", nameof(targetCurrency));
            if (rate <= 0m)
                throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be greater than 0.");

            if (string.IsNullOrWhiteSpace(_tx.Current.Info.TransactionId))
                _tx.Start(ServiceType.Exchange, "KRW");

            var flow = _resolver.Resolve(_selectedType, _selectedPayoutMethod);
            _tx.SetFunding(flow.FundingType);
            _tx.SetComplianceRequirements(flow.RequiresIdentity, flow.RequiresLimitCheck);

            var sourceCurrency = string.IsNullOrWhiteSpace(_tx.Current.Funding.DepositCurrency)
                ? _tx.Current.Info.LocalCurrency
                : _tx.Current.Funding.DepositCurrency;

            _tx.SetRateAndPolicy(
                new DomainExchangeRateInfo
                {
                    SourceCurrency = sourceCurrency,
                    TargetCurrency = targetCurrency.Trim().ToUpperInvariant(),
                    Rate = rate
                },
                new DomainExchangePolicyInfo
                {
                    FeePercent = 0m,
                    FeeFlat = 0m,
                    RoundingUnit = 1m,
                    RoundingMode = TransactionRoundingMode.Down
                });

            return Task.CompletedTask;
        }
    }
}
