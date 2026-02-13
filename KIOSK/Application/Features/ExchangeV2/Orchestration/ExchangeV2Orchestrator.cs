using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Application.Features.ExchangeV2.UseCases;
using KIOSK.Application.Services.Exchange;
using KIOSK.Domain.Transactions;

namespace KIOSK.Application.Features.ExchangeV2.Orchestration
{
    public sealed class ExchangeV2Orchestrator : IExchangeV2Orchestrator
    {
        private readonly IExchangeSelectLanguageUseCase _languageUseCase;
        private readonly IExchangeV2PolicyUseCase _policyUseCase;
        private readonly IExchangeV2ComplianceUseCase _complianceUseCase;
        private readonly IExchangeV2DepositUseCase _depositUseCase;
        private readonly IExchangeV2PayoutUseCase _payoutUseCase;

        public ExchangeV2Orchestrator(
            IExchangeSelectLanguageUseCase languageUseCase,
            IExchangeV2PolicyUseCase policyUseCase,
            IExchangeV2ComplianceUseCase complianceUseCase,
            IExchangeV2DepositUseCase depositUseCase,
            IExchangeV2PayoutUseCase payoutUseCase)
        {
            _languageUseCase = languageUseCase;
            _policyUseCase = policyUseCase;
            _complianceUseCase = complianceUseCase;
            _depositUseCase = depositUseCase;
            _payoutUseCase = payoutUseCase;
        }

        public Task SelectLanguageAsync(string languageCode, CancellationToken ct = default)
            => _languageUseCase.SelectAsync(languageCode, ct);

        public void SelectTransactionType(ExchangeTransactionType type)
            => _policyUseCase.SelectTransactionType(type);

        public void SelectPayoutMethod(PayoutMethodType method)
            => _policyUseCase.SelectPayoutMethod(method);

        public Task ApplyCurrencyAsync(string targetCurrency, decimal rate, CancellationToken ct = default)
            => _policyUseCase.ApplyCurrencyAsync(targetCurrency, rate, ct);

        public Task<bool> ProcessIdentityAsync(CancellationToken ct = default)
            => _complianceUseCase.ProcessIdentityAsync(ct);

        public Task<ExchangeLimitInfo> InquireLimitAsync(CancellationToken ct = default)
            => _complianceUseCase.InquireLimitAsync(ct);

        public Task<bool> ProcessIdentityAndLimitAsync(CancellationToken ct = default)
            => _complianceUseCase.ProcessIdentityAndLimitAsync(ct);

        public Task StartDepositAsync(CancellationToken ct = default)
            => _depositUseCase.StartAsync(ct);

        public Task StopDepositAsync(CancellationToken ct = default)
            => _depositUseCase.StopAsync(ct);

        public DepositApplyResult TryApplyDeposit(string currency, decimal denomination, int deltaCount = 1)
            => _depositUseCase.TryApplyDeposit(currency, denomination, deltaCount);

        public void PlanPayout(IReadOnlyCollection<ExchangeV2PayoutRequest> requests)
            => _payoutUseCase.ApplyPlan(requests);

        public Task ExecutePayoutAsync(CancellationToken ct = default)
            => _payoutUseCase.ExecuteAsync(ct);
    }
}
