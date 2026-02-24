using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Application.Services;
using KIOSK.Application.Services.Devices;
using KIOSK.Domain.Transactions;

namespace KIOSK.Application.Features.ExchangeV2.UseCases
{
    public interface IExchangeV2PayoutUseCase
    {
        void ApplyPlan(IReadOnlyCollection<ExchangeV2PayoutRequest> requests);
        Task ExecuteAsync(CancellationToken ct);
    }

    public sealed class ExchangeV2PayoutUseCase : IExchangeV2PayoutUseCase
    {
        private readonly IExchangeV2TransactionContext _tx;
        private readonly IExchangeV2FlowPolicyResolver _resolver;
        private readonly IExchangeV2FlowPolicyValidator _validator;
        private readonly WithdrawalCassetteService _cassetteService;
        private readonly IWithdrawalPort _withdrawalDeviceService;

        public ExchangeV2PayoutUseCase(
            IExchangeV2TransactionContext tx,
            IExchangeV2FlowPolicyResolver resolver,
            IExchangeV2FlowPolicyValidator validator,
            WithdrawalCassetteService cassetteService,
            IWithdrawalPort withdrawalDeviceService)
        {
            _tx = tx;
            _resolver = resolver;
            _validator = validator;
            _cassetteService = cassetteService;
            _withdrawalDeviceService = withdrawalDeviceService;
        }

        public void ApplyPlan(IReadOnlyCollection<ExchangeV2PayoutRequest> requests)
        {
            if (requests is null || requests.Count == 0)
                throw new ArgumentException("At least one payout request is required.", nameof(requests));

            var positive = requests.Where(x => x.Amount > 0m).ToList();
            if (positive.Count == 0)
                throw new ArgumentException("At least one positive payout amount is required.", nameof(requests));

            var total = positive.Sum(x => x.Amount);
            var baseAmount = _tx.Current.Conversion?.NetAmount
                             ?? (_tx.Current.Funding.Type == FundingType.EasyPay
                                 ? _tx.Current.Funding.RequestedPayAmount
                                 : _tx.Current.Funding.DepositTotalAmount);

            if (baseAmount > 0m && total > baseAmount)
                throw new InvalidOperationException("Requested payout exceeds available amount.");

            var allocation = new AllocationPlan
            {
                Items = positive.Select(MapAllocation).ToList()
            };

            var selectedMethod = positive.Count == 1 ? positive[0].Method : PayoutMethodType.Cash;
            var policy = _resolver.Resolve(_tx.SelectedTransactionType, selectedMethod);
            _validator.ValidateAllocation(policy, allocation);

            _tx.SetAllocation(allocation);
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            await _cassetteService.InitializeAsync(ct).ConfigureAwait(false);

            var cashAmount = _tx.Current.Allocation.CashAmount;
            var cashCurrency = ResolveCashCurrency();
            var plannedLines = PlanCash(cashCurrency, cashAmount, _cassetteService.Get());

            var grouped = plannedLines
                .GroupBy(x => x.DeviceId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var group in grouped)
            {
                var payload = BuildPayload(group);
                var resultMap = await _withdrawalDeviceService
                    .DispenseAsync(group.Key, payload, ct)
                    .ConfigureAwait(false);
                ApplyResults(group.ToList(), resultMap);
            }

            await PersistCassetteResultAsync(plannedLines, ct).ConfigureAwait(false);
            _tx.SetFulfillment(BuildFulfillment(plannedLines));
        }

        private FulfillmentResult BuildFulfillment(IReadOnlyCollection<PlannedCashLine> plannedLines)
        {
            var cashDetails = plannedLines
                .Select(x => new CashResultItem
                {
                    DeviceId = x.DeviceId,
                    Slot = x.Slot,
                    Currency = x.Currency,
                    Denomination = x.Denomination,
                    PlannedCount = x.RequestedCount,
                    SuccessCount = x.SucceededCount,
                    FailedCount = x.FailedCount,
                    RejectedCount = x.RejectedCount
                })
                .ToList();

            var hasCashFailure = cashDetails.Any(x => x.FailedCount > 0 || x.RejectedCount > 0);
            var hasCardRequest = _tx.Current.Allocation.TransitAmount > 0m || _tx.Current.Allocation.PrepaidAmount > 0m;

            var cardError = hasCardRequest ? "CARD_CHANNEL_NOT_IMPLEMENTED" : string.Empty;
            var cashError = hasCashFailure ? "CASH_PAYOUT_PARTIAL" : string.Empty;

            return new FulfillmentResult
            {
                Cash = new CashResult
                {
                    Details = cashDetails,
                    ErrorCode = cashError
                },
                Card = new CardResult
                {
                    Prepaid = BuildPlannedWallet(_tx.Current.Allocation.PrepaidAmount, cardError),
                    Transit = BuildPlannedWallet(_tx.Current.Allocation.TransitAmount, cardError),
                    IsSuccess = !hasCardRequest,
                    ErrorCode = cardError
                },
                IsSuccess = !hasCardRequest && !hasCashFailure,
                ErrorCode = hasCardRequest ? cardError : cashError
            };
        }

        private static WalletResult? BuildPlannedWallet(decimal plannedAmount, string errorCode)
        {
            if (plannedAmount <= 0m)
                return null;

            return new WalletResult
            {
                PlannedTopupAmount = plannedAmount,
                CompletedTopupAmount = 0m,
                IsSuccess = false,
                ErrorCode = errorCode
            };
        }

        private static AllocationItem MapAllocation(ExchangeV2PayoutRequest request)
        {
            var channel = request.Method switch
            {
                PayoutMethodType.Cash => AllocationChannel.Cash,
                PayoutMethodType.PrepaidCard => AllocationChannel.Prepaid,
                PayoutMethodType.TransitCard => AllocationChannel.Transit,
                _ => AllocationChannel.Cash
            };

            return new AllocationItem
            {
                Channel = channel,
                Amount = request.Amount
            };
        }

        private string ResolveCashCurrency()
        {
            if (!string.IsNullOrWhiteSpace(_tx.Current.ExchangeRate?.TargetCurrency))
                return _tx.Current.ExchangeRate.TargetCurrency.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(_tx.Current.Info.LocalCurrency))
                return _tx.Current.Info.LocalCurrency.Trim().ToUpperInvariant();

            return "KRW";
        }

        private static List<PlannedCashLine> PlanCash(
            string currency,
            decimal amount,
            IEnumerable<WithdrawalCassette> stock)
        {
            var planned = new List<PlannedCashLine>();
            if (amount <= 0m)
                return planned;

            var remaining = amount;
            var candidates = stock
                .Where(x => x.CurrencyCode.Equals(currency, StringComparison.OrdinalIgnoreCase) && x.Denomination > 0m && x.Count > 0)
                .OrderByDescending(x => x.Denomination)
                .ThenBy(x => x.DeviceID, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Slot)
                .ToList();

            foreach (var cassette in candidates)
            {
                if (remaining < cassette.Denomination)
                    continue;

                var needed = (int)(remaining / cassette.Denomination);
                if (needed <= 0)
                    continue;

                var use = Math.Min(needed, cassette.Count);
                if (use <= 0)
                    continue;

                planned.Add(new PlannedCashLine
                {
                    DeviceId = cassette.DeviceID,
                    Slot = cassette.Slot,
                    Currency = cassette.CurrencyCode,
                    Denomination = cassette.Denomination,
                    RequestedCount = use
                });

                remaining -= cassette.Denomination * use;
            }

            return planned;
        }

        private static byte[] BuildPayload(IEnumerable<PlannedCashLine> lines)
        {
            var payload = new byte[14];
            foreach (var line in lines)
            {
                if (line.Slot < 1 || line.Slot > 6)
                    continue;

                payload[line.Slot - 1] = (byte)Math.Clamp(line.RequestedCount, 0, 150);
            }

            return payload;
        }

        private static void ApplyResults(List<PlannedCashLine> lines, Dictionary<int, (int req, int exit, int rej)>? resultMap)
        {
            foreach (var line in lines)
            {
                if (resultMap is null || !resultMap.TryGetValue(line.Slot - 1, out var result))
                {
                    line.SucceededCount = 0;
                    line.RejectedCount = 0;
                    line.FailedCount = line.RequestedCount;
                    continue;
                }

                line.SucceededCount = Math.Max(0, result.exit);
                line.RejectedCount = Math.Max(0, result.rej);
                var unresolved = line.RequestedCount - line.SucceededCount - line.RejectedCount;
                line.FailedCount = Math.Max(0, unresolved);
            }
        }

        private async Task PersistCassetteResultAsync(IReadOnlyCollection<PlannedCashLine> lines, CancellationToken ct)
        {
            var succeeded = lines
                .Where(x => x.SucceededCount > 0)
                .Select(x => (x.DeviceId, x.Currency, x.Slot, x.Denomination, x.SucceededCount))
                .ToList();

            if (succeeded.Count > 0)
                await _cassetteService.WithdrawalAsync(succeeded, ct).ConfigureAwait(false);

            var json = JsonSerializer.Serialize(_tx.Current);
            await _cassetteService.ResultAsync(json, ct).ConfigureAwait(false);
        }

        private sealed class PlannedCashLine
        {
            public string DeviceId { get; set; } = string.Empty;
            public int Slot { get; set; }
            public string Currency { get; set; } = string.Empty;
            public decimal Denomination { get; set; }
            public int RequestedCount { get; set; }
            public int SucceededCount { get; set; }
            public int FailedCount { get; set; }
            public int RejectedCount { get; set; }
        }
    }
}
