using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Application.Services.Exchange;
using KIOSK.Domain.Transactions;

namespace KIOSK.Application.Features.ExchangeV2.UseCases
{
    public interface IExchangeV2DepositUseCase
    {
        Task StartAsync(CancellationToken ct);
        Task StopAsync(CancellationToken ct);
        DepositApplyResult TryApplyDeposit(string currency, decimal denomination, int deltaCount = 1);
    }

    public sealed class ExchangeV2DepositUseCase : IExchangeV2DepositUseCase
    {
        private readonly IExchangeV2TransactionContext _tx;
        private readonly IExchangeDepositSessionService _session;

        public ExchangeV2DepositUseCase(
            IExchangeV2TransactionContext tx,
            IExchangeDepositSessionService session)
        {
            _tx = tx;
            _session = session;
        }

        public Task StartAsync(CancellationToken ct) => _session.StartAsync(ct);
        public Task StopAsync(CancellationToken ct) => _session.StopAsync(ct);

        public DepositApplyResult TryApplyDeposit(string currency, decimal denomination, int deltaCount = 1)
        {
            if (string.IsNullOrWhiteSpace(currency) || denomination <= 0m || deltaCount <= 0)
            {
                return new DepositApplyResult(
                    DepositApplyStatus.ReturnedInvalidInput,
                    _tx.Current.Funding.DepositTotalAmount,
                    GetProjectedAmount(_tx.Current.Funding.DepositTotalAmount),
                    "INVALID_INPUT",
                    "Invalid deposit note input.");
            }

            if (!string.IsNullOrWhiteSpace(_tx.Current.Funding.DepositCurrency) &&
                !_tx.Current.Funding.DepositCurrency.Equals(currency, StringComparison.OrdinalIgnoreCase))
            {
                return new DepositApplyResult(
                    DepositApplyStatus.ReturnedCurrencyMismatch,
                    _tx.Current.Funding.DepositTotalAmount,
                    GetProjectedAmount(_tx.Current.Funding.DepositTotalAmount),
                    "CURRENCY_MISMATCH",
                    "Currency does not match current transaction source currency.");
            }

            var projectedSource = _tx.Current.Funding.DepositTotalAmount + (denomination * deltaCount);
            var projectedConverted = GetProjectedAmount(projectedSource);

            var limit = _tx.Current.Compliance.Limit;
            if (_tx.Current.Compliance.RequiresLimitCheck && limit is not null && limit.IsChecked)
            {
                if (!limit.IsApproved)
                {
                    return new DepositApplyResult(
                        DepositApplyStatus.ReturnedLimitNotApproved,
                        projectedSource,
                        projectedConverted,
                        string.IsNullOrWhiteSpace(limit.ReasonCode) ? "LIMIT_NOT_APPROVED" : limit.ReasonCode,
                        string.IsNullOrWhiteSpace(limit.Message) ? "Limit check is not approved." : limit.Message);
                }

                if (projectedConverted > limit.AppliedMaxAmount)
                {
                    return new DepositApplyResult(
                        DepositApplyStatus.ReturnedLimitExceeded,
                        projectedSource,
                        projectedConverted,
                        "LIMIT_EXCEEDED",
                        "Projected amount exceeds exchange limit.");
                }
            }

            _tx.SetFunding(FundingType.CashDeposit, currency.Trim().ToUpperInvariant());
            _tx.AddDeposit(denomination, deltaCount);
            _tx.SetConversion(new ConversionInfo
            {
                GrossAmount = _tx.Current.Funding.DepositTotalAmount * GetRate(),
                NetAmount = GetProjectedAmount(_tx.Current.Funding.DepositTotalAmount)
            });

            return new DepositApplyResult(
                DepositApplyStatus.Stacked,
                _tx.Current.Funding.DepositTotalAmount,
                _tx.Current.Conversion?.NetAmount ?? 0m,
                string.Empty,
                string.Empty);
        }

        private decimal GetProjectedAmount(decimal sourceAmount)
        {
            var rate = GetRate();
            if (rate <= 0m)
                return 0m;

            var gross = sourceAmount * rate;
            var policy = _tx.Current.ExchangePolicy;
            if (policy is null)
                return gross;

            var fee = (gross * policy.FeePercent) + policy.FeeFlat;
            var net = Math.Max(0m, gross - fee);
            return ApplyRounding(net, policy.RoundingUnit, policy.RoundingMode);
        }

        private decimal GetRate() => _tx.Current.ExchangeRate?.Rate ?? 0m;

        private static decimal ApplyRounding(decimal amount, decimal unit, TransactionRoundingMode mode)
        {
            if (unit <= 0m)
                return amount;

            var units = amount / unit;
            return mode switch
            {
                TransactionRoundingMode.Down => Math.Floor(units) * unit,
                TransactionRoundingMode.Up => Math.Ceiling(units) * unit,
                TransactionRoundingMode.Nearest => Math.Round(units, MidpointRounding.AwayFromZero) * unit,
                _ => amount
            };
        }
    }
}
