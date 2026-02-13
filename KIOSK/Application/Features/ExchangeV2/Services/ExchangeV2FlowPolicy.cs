using System;
using KIOSK.Domain.Transactions;

namespace KIOSK.Application.Features.ExchangeV2.Services
{
    public sealed record ExchangeV2FlowPolicy
    {
        public ServiceType ServiceType { get; init; }
        public FundingType FundingType { get; init; }
        public bool RequiresIdentity { get; init; }
        public bool RequiresLimitCheck { get; init; }
        public bool RequiresConversion { get; init; }
        public bool AllowCash { get; init; }
        public bool AllowPrepaid { get; init; }
        public bool AllowTransit { get; init; }
        public bool AllocationUsesNetAmount { get; init; }
    }

    public interface IExchangeV2FlowPolicyResolver
    {
        ExchangeV2FlowPolicy Resolve(ExchangeTransactionType transactionType, PayoutMethodType payoutMethod);
    }

    public sealed class ExchangeV2FlowPolicyResolver : IExchangeV2FlowPolicyResolver
    {
        public ExchangeV2FlowPolicy Resolve(ExchangeTransactionType transactionType, PayoutMethodType payoutMethod)
        {
            if (transactionType is not ExchangeTransactionType.Sell and not ExchangeTransactionType.Buy)
                throw new InvalidOperationException($"Unsupported transaction type: {transactionType}");

            var basePolicy = new ExchangeV2FlowPolicy
            {
                ServiceType = ServiceType.Exchange,
                FundingType = FundingType.CashDeposit,
                RequiresIdentity = true,
                RequiresLimitCheck = true,
                RequiresConversion = true,
                AllocationUsesNetAmount = true
            };

            var policy = payoutMethod switch
            {
                PayoutMethodType.Cash => basePolicy with
                {
                    AllowCash = true,
                    AllowPrepaid = false,
                    AllowTransit = false
                },
                PayoutMethodType.PrepaidCard => basePolicy with
                {
                    AllowCash = false,
                    AllowPrepaid = true,
                    AllowTransit = false
                },
                PayoutMethodType.TransitCard => basePolicy with
                {
                    AllowCash = false,
                    AllowPrepaid = false,
                    AllowTransit = true
                },
                _ => basePolicy
            };

            return transactionType switch
            {
                ExchangeTransactionType.Sell => policy,
                ExchangeTransactionType.Buy => policy with
                {
                    ServiceType = ServiceType.Exchange
                },
                _ => policy
            };
        }
    }

    public interface IExchangeV2FlowPolicyValidator
    {
        void ValidateAllocation(ExchangeV2FlowPolicy policy, AllocationPlan allocation);
    }

    public sealed class ExchangeV2FlowPolicyValidator : IExchangeV2FlowPolicyValidator
    {
        public void ValidateAllocation(ExchangeV2FlowPolicy policy, AllocationPlan allocation)
        {
            if (!policy.AllowCash && allocation.CashAmount > 0m)
                throw new InvalidOperationException("Cash allocation is not allowed in current flow.");

            if (!policy.AllowTransit && allocation.TransitAmount > 0m)
                throw new InvalidOperationException("Transit allocation is not allowed in current flow.");

            if (!policy.AllowPrepaid && allocation.PrepaidAmount > 0m)
                throw new InvalidOperationException("Prepaid allocation is not allowed in current flow.");
        }
    }
}
