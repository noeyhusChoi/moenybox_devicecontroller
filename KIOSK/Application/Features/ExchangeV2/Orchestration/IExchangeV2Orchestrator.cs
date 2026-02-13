using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Application.Features.ExchangeV2.UseCases;
using KIOSK.Application.Services.Exchange;
using KIOSK.Domain.Transactions;

namespace KIOSK.Application.Features.ExchangeV2.Orchestration
{
    public interface IExchangeV2Orchestrator
    {
        Task SelectLanguageAsync(string languageCode, CancellationToken ct = default);
        void SelectTransactionType(ExchangeTransactionType type);
        void SelectPayoutMethod(PayoutMethodType method);
        Task ApplyCurrencyAsync(string targetCurrency, decimal rate, CancellationToken ct = default);

        Task<bool> ProcessIdentityAsync(CancellationToken ct = default);
        Task<ExchangeLimitInfo> InquireLimitAsync(CancellationToken ct = default);
        Task<bool> ProcessIdentityAndLimitAsync(CancellationToken ct = default);

        Task StartDepositAsync(CancellationToken ct = default);
        Task StopDepositAsync(CancellationToken ct = default);
        DepositApplyResult TryApplyDeposit(string currency, decimal denomination, int deltaCount = 1);
        void PlanPayout(IReadOnlyCollection<ExchangeV2PayoutRequest> requests);
        Task ExecutePayoutAsync(CancellationToken ct = default);
    }
}
