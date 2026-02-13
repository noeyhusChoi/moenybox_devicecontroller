using KIOSK.Domain.Transactions;

namespace KIOSK.Application.Features.ExchangeV2.Services
{
    public interface IExchangeV2TransactionContext
    {
        CommerceTransaction Current { get; }
        ExchangeTransactionType SelectedTransactionType { get; }
        PayoutMethodType SelectedPayoutMethod { get; }

        void Start(ServiceType serviceType, string localCurrency);
        void SelectTransactionType(ExchangeTransactionType type);
        void SelectPayoutMethod(PayoutMethodType method);
        void SelectCardFeatures(CardFeature features);
        void SetFunding(FundingType type, string? depositCurrency = null, string? easyPayProvider = null);
        void SetComplianceRequirements(bool requiresIdentity, bool requiresLimitCheck);
        void SetCustomer(CustomerIdentity customer);
        void SetLimit(ExchangeLimitInfo limit);
        void SetRateAndPolicy(ExchangeRateInfo rate, ExchangePolicyInfo policy);
        void AddDeposit(decimal denomination, int count);
        void SetConversion(ConversionInfo conversion);
        void SetAllocation(AllocationPlan allocation);
        void SetFulfillment(FulfillmentResult fulfillment);
    }
}
