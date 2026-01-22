using System;
using System.Collections.Generic;

namespace KIOSK.Domain.Entities
{
    public enum ExchangeTransactionType
    {
        Unknown,
        BuyFX,
        SellFX
    }

    public sealed class TransactionInfo
    {
        public DateTime TransactionTime { get; set; } = DateTime.Now;
        public string TransactionId { get; set; } = string.Empty;
        public ExchangeTransactionType TransactionType { get; set; } = ExchangeTransactionType.Unknown;
    }

    public sealed class ExchangeRateInfo
    {
        public string SourceCurrency { get; set; } = string.Empty;
        public string TargetCurrency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime? BaseDateTime { get; set; }
    }

    public enum ExchangeRoundingMode
    {
        Down,
        Up,
        Nearest
    }

    public sealed class ExchangePolicyInfo
    {
        public decimal FeePercent { get; set; }
        public decimal FeeFlat { get; set; }
        public decimal TargetIncrement { get; set; }
        public ExchangeRoundingMode RoundingMode { get; set; } = ExchangeRoundingMode.Down;
    }

    public sealed class DepositItem
    {
        public decimal Denomination { get; set; }
        public int Count { get; set; }
        public decimal Amount => Denomination * Count;
    }

    public sealed class DepositInfo
    {
        public string Currency { get; set; } = string.Empty;
        public List<DepositItem> Items { get; } = new();
        public decimal TotalAmount { get; set; }
    }

    public enum PayoutMethodType
    {
        Cash,
        TransitCard,
        PrepaidCard
    }

    public sealed class CashPayoutItem
    {
        public decimal Denomination { get; set; }
        public int Count { get; set; }
        public decimal Amount => Denomination * Count;
    }

    public sealed class CashPayoutDetail
    {
        public List<CashPayoutItem> Items { get; } = new();
    }

    public sealed class TransitCardTopupDetail
    {
        public string CardId { get; set; } = string.Empty;
        public string ApprovalNo { get; set; } = string.Empty;
        public decimal BalanceAfter { get; set; }
    }

    public sealed class PrepaidCardTopupDetail
    {
        public string CardId { get; set; } = string.Empty;
        public string ApprovalNo { get; set; } = string.Empty;
        public decimal BalanceAfter { get; set; }
    }

    public sealed class PayoutMethodInfo
    {
        public PayoutMethodType Method { get; set; }
        public decimal PlannedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public CashPayoutDetail? CashDetail { get; set; }
        public TransitCardTopupDetail? TransitCardDetail { get; set; }
        public PrepaidCardTopupDetail? PrepaidCardDetail { get; set; }
    }

    public sealed class PayoutInfo
    {
        public decimal PlannedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public List<PayoutMethodInfo> Methods { get; } = new();
    }

    public sealed class ExchangeTransaction
    {
        public TransactionInfo Info { get; set; } = new();
        public CustomerInfo Customer { get; set; } = new();
        public DepositInfo Deposit { get; set; } = new();
        public PayoutInfo Payout { get; set; } = new();
        public ExchangeRateInfo Rate { get; set; } = new();
        public ExchangePolicyInfo Policy { get; set; } = new();
    }
}
