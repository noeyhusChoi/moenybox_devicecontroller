using KIOSK.Domain.Entities;
using System;
using System.Linq;

namespace KIOSK.Application.Services.ExchangeV2
{
    public sealed class ExchangeV2TransactionService : IExchangeV2TransactionContext
    {
        public ExchangeTransaction Current { get; private set; } = new();

        public void Start(ExchangeTransactionType type)
        {
            Current = new ExchangeTransaction
            {
                Info = new TransactionInfo
                {
                    TransactionTime = DateTime.Now,
                    TransactionId = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TransactionType = type
                },
                Customer = new CustomerInfo(),
                Deposit = new DepositInfo(),
                Payout = new PayoutInfo(),
                Rate = new ExchangeRateInfo(),
                Policy = new ExchangePolicyInfo()
            };
        }

        public void SetTransactionType(ExchangeTransactionType type)
        {
            Current.Info.TransactionType = type;
        }

        public void SetCustomer(CustomerInfo customer)
        {
            Current.Customer = customer ?? new CustomerInfo();
        }

        public void SetDeposit(DepositInfo deposit)
        {
            Current.Deposit = deposit ?? new DepositInfo();
        }

        public void SetPayout(PayoutInfo payout)
        {
            Current.Payout = payout ?? new PayoutInfo();
        }

        public void SetRate(ExchangeRateInfo rate)
        {
            Current.Rate = rate ?? new ExchangeRateInfo();
        }

        public void SetPolicy(ExchangePolicyInfo policy)
        {
            Current.Policy = policy ?? new ExchangePolicyInfo();
        }

        public void AddDeposit(string currency, decimal denomination, int deltaCount = 1)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException(nameof(currency));

            if (deltaCount <= 0)
                return;

            var item = Current.Deposit.Items.Find(x => x.Denomination == denomination);
            if (item == null)
            {
                item = new DepositItem
                {
                    Denomination = denomination,
                    Count = 0
                };
                Current.Deposit.Items.Add(item);
            }

            item.Count += deltaCount;
            Current.Deposit.Currency = currency;
            Current.Deposit.TotalAmount = Current.Deposit.Items.Sum(x => x.Amount);

            RecalculateComputedAmounts();
        }

        public void RecalculateComputedAmounts()
        {
            var rate = Current.Rate.Rate;
            if (rate <= 0m)
            {
                Current.Payout.PlannedAmount = 0m;
                return;
            }

            var gross = Current.Deposit.TotalAmount * rate;
            var rounded = ApplyRounding(gross, Current.Policy.TargetIncrement, Current.Policy.RoundingMode);
            Current.Payout.PlannedAmount = rounded;
        }

        private static decimal ApplyRounding(decimal amount, decimal increment, ExchangeRoundingMode mode)
        {
            if (increment <= 0m)
                return amount;

            var units = amount / increment;
            return mode switch
            {
                ExchangeRoundingMode.Down => Math.Floor(units) * increment,
                ExchangeRoundingMode.Up => Math.Ceiling(units) * increment,
                ExchangeRoundingMode.Nearest => Math.Round(units, MidpointRounding.AwayFromZero) * increment,
                _ => amount
            };
        }
    }
}
