using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Domain.Entities;
using KIOSK.Domain.Transactions;

namespace KIOSK.Application.Features.ExchangeV2.UseCases
{
    public interface IExchangeV2ComplianceUseCase
    {
        Task<bool> ProcessIdentityAsync(CancellationToken ct = default);
        Task<ExchangeLimitInfo> InquireLimitAsync(CancellationToken ct = default);
        Task<bool> ProcessIdentityAndLimitAsync(CancellationToken ct = default);
    }

    public sealed class ExchangeV2ComplianceUseCase : IExchangeV2ComplianceUseCase
    {
        private readonly IExchangeV2IdScanUseCase _idScan;
        private readonly IExchangeV2TransactionContext _tx;
        private readonly IExchangeV2LimitProvider _limitProvider;

        public ExchangeV2ComplianceUseCase(
            IExchangeV2IdScanUseCase idScan,
            IExchangeV2TransactionContext tx,
            IExchangeV2LimitProvider limitProvider)
        {
            _idScan = idScan;
            _tx = tx;
            _limitProvider = limitProvider;
        }

        public async Task<bool> ProcessIdentityAndLimitAsync(CancellationToken ct = default)
        {
            var ok = await ProcessIdentityAsync(ct).ConfigureAwait(false);
            if (!ok)
                return false;

            if (!_tx.Current.Compliance.RequiresLimitCheck)
                return true;

            var limit = await InquireLimitAsync(ct).ConfigureAwait(false);
            return limit.IsApproved;
        }

        public async Task<bool> ProcessIdentityAsync(CancellationToken ct = default)
        {
            var ok = await _idScan.ProcessAsync(ct).ConfigureAwait(false);
            return ok;
        }

        public async Task<ExchangeLimitInfo> InquireLimitAsync(CancellationToken ct = default)
        {
            var customer = _tx.Current.Compliance.Customer;
            if (customer is null || string.IsNullOrWhiteSpace(customer.IdNumber))
                throw new InvalidOperationException("CustomerNumber is required before limit inquiry.");

            var requestCustomer = new CustomerInfo
            {
                IdType = customer.IdType,
                CustomerName = customer.Name,
                CustomerNumber = customer.IdNumber,
                CustomerNationality = customer.Nationality
            };

            var result = await _limitProvider.InquireAsync(requestCustomer, ct).ConfigureAwait(false);
            var limit = new ExchangeLimitInfo
            {
                IsChecked = true,
                IsApproved = result.IsApproved,
                DailyLimitAmount = result.DailyLimitAmount,
                DailyRemainingAmount = Math.Max(0m, result.DailyRemainingAmount),
                PerTransactionLimitAmount = Math.Max(0m, result.PerTransactionLimitAmount),
                ReasonCode = result.ReasonCode,
                Message = result.Message,
                CheckedAt = DateTimeOffset.UtcNow
            };

            _tx.SetLimit(limit);
            return limit;
        }
    }
}
