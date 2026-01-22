using KIOSK.Domain.Entities;

namespace KIOSK.Application.Services.ExchangeV2
{
    public interface IExchangeV2TransactionContext
    {
        ExchangeTransaction Current { get; }
        void Start(ExchangeTransactionType type);
        void SetTransactionType(ExchangeTransactionType type);
        void SetCustomer(CustomerInfo customer);
        void SetDeposit(DepositInfo deposit);
        void SetPayout(PayoutInfo payout);
        void SetRate(ExchangeRateInfo rate);
        void SetPolicy(ExchangePolicyInfo policy);
        void AddDeposit(string currency, decimal denomination, int deltaCount = 1);
        void RecalculateComputedAmounts();
    }
}
