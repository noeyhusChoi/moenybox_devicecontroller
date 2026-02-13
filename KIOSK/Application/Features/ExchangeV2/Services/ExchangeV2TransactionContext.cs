using KIOSK.Domain.Transactions;
using System;
using System.Linq;

namespace KIOSK.Application.Features.ExchangeV2.Services
{
    public sealed class ExchangeV2TransactionContext : IExchangeV2TransactionContext
    {
        public CommerceTransaction Current { get; private set; } = new();
        public ExchangeTransactionType SelectedTransactionType { get; private set; } = ExchangeTransactionType.Sell;
        public PayoutMethodType SelectedPayoutMethod { get; private set; } = PayoutMethodType.Cash;

        public void Start(ServiceType serviceType, string localCurrency)
        {
            var now = DateTimeOffset.UtcNow;
            Current = new CommerceTransaction
            {
                ServiceType = serviceType,
                Info = new TransactionInfo
                {
                    TransactionId = now.ToString("yyyyMMddHHmmss"),
                    TransactionTime = now,
                    LocalCurrency = localCurrency
                }
            };
        }

        public void SelectTransactionType(ExchangeTransactionType type)
        {
            SelectedTransactionType = type;
        }

        public void SelectPayoutMethod(PayoutMethodType method)
        {
            SelectedPayoutMethod = method;
        }

        public void SelectCardFeatures(CardFeature features)
        {
            Current = CloneCurrent(selectedCardFeatures: features);
        }

        public void SetFunding(FundingType type, string? depositCurrency = null, string? easyPayProvider = null)
        {
            var funding = CloneFunding(Current.Funding);
            funding.Type = type;
            if (!string.IsNullOrWhiteSpace(depositCurrency))
                funding.DepositCurrency = depositCurrency;
            if (!string.IsNullOrWhiteSpace(easyPayProvider))
                funding.EasyPayProvider = easyPayProvider;

            Current = CloneCurrent(funding: funding);
        }

        public void SetCustomer(CustomerIdentity customer)
        {
            var compliance = CloneCompliance(Current.Compliance);
            compliance.Customer = customer;
            Current = CloneCurrent(compliance: compliance);
        }

        public void SetComplianceRequirements(bool requiresIdentity, bool requiresLimitCheck)
        {
            var compliance = CloneCompliance(Current.Compliance);
            compliance.RequiresIdentity = requiresIdentity;
            compliance.RequiresLimitCheck = requiresLimitCheck;
            Current = CloneCurrent(compliance: compliance);
        }

        public void SetLimit(ExchangeLimitInfo limit)
        {
            var compliance = CloneCompliance(Current.Compliance);
            compliance.Limit = limit;
            Current = CloneCurrent(compliance: compliance);
        }

        public void SetRateAndPolicy(ExchangeRateInfo rate, ExchangePolicyInfo policy)
        {
            Current = CloneCurrent(exchangeRate: rate, exchangePolicy: policy);
        }

        public void AddDeposit(decimal denomination, int count)
        {
            if (denomination <= 0m || count <= 0)
                return;

            var items = Current.Funding.DepositItems.ToList();
            var found = items.FirstOrDefault(x => x.Denomination == denomination);
            if (found is null)
            {
                items.Add(new DepositItem
                {
                    Denomination = denomination,
                    Count = count
                });
            }
            else
            {
                items.Remove(found);
                items.Add(new DepositItem
                {
                    Denomination = found.Denomination,
                    Count = found.Count + count
                });
            }

            var funding = CloneFunding(Current.Funding);
            funding.DepositItems = items;
            Current = CloneCurrent(funding: funding);
        }

        public void SetConversion(ConversionInfo conversion)
        {
            Current = CloneCurrent(conversion: conversion);
        }

        public void SetAllocation(AllocationPlan allocation)
        {
            Current = CloneCurrent(allocation: allocation);
        }

        public void SetFulfillment(FulfillmentResult fulfillment)
        {
            Current = CloneCurrent(fulfillment: fulfillment);
        }

        private CommerceTransaction CloneCurrent(
            TransactionInfo? info = null,
            ServiceType? serviceType = null,
            CardFeature? selectedCardFeatures = null,
            FundingInfo? funding = null,
            ComplianceInfo? compliance = null,
            ExchangeRateInfo? exchangeRate = null,
            ExchangePolicyInfo? exchangePolicy = null,
            ConversionInfo? conversion = null,
            AllocationPlan? allocation = null,
            FulfillmentResult? fulfillment = null)
        {
            return new CommerceTransaction
            {
                Info = info ?? Current.Info,
                ServiceType = serviceType ?? Current.ServiceType,
                SelectedCardFeatures = selectedCardFeatures ?? Current.SelectedCardFeatures,
                Funding = funding ?? Current.Funding,
                Compliance = compliance ?? Current.Compliance,
                ExchangeRate = exchangeRate ?? Current.ExchangeRate,
                ExchangePolicy = exchangePolicy ?? Current.ExchangePolicy,
                Conversion = conversion ?? Current.Conversion,
                Allocation = allocation ?? Current.Allocation,
                Fulfillment = fulfillment ?? Current.Fulfillment
            };
        }

        private static FundingInfo CloneFunding(FundingInfo source)
        {
            return new FundingInfo
            {
                Type = source.Type,
                DepositCurrency = source.DepositCurrency,
                DepositItems = source.DepositItems.ToList(),
                RequestedPayAmount = source.RequestedPayAmount,
                EasyPayProvider = source.EasyPayProvider
            };
        }

        private static ComplianceInfo CloneCompliance(ComplianceInfo source)
        {
            return new ComplianceInfo
            {
                RequiresIdentity = source.RequiresIdentity,
                RequiresLimitCheck = source.RequiresLimitCheck,
                Customer = source.Customer,
                Limit = source.Limit
            };
        }
    }
}
