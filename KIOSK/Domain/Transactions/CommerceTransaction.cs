using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiosk.Domain.Transactions
{
    [Flags]
    public enum CardFeature
    {
        None = 0,
        Transit = 1,
        Prepaid = 2
    }

    public enum ServiceType
    {
        Exchange,
        CardPurchaseAndTopup,
        CardTopup
    }

    public enum FundingType
    {
        CashDeposit,
        EasyPay
    }

    public enum AllocationChannel
    {
        Cash,
        Transit,
        Prepaid
    }

    public enum TransactionRoundingMode
    {
        Down,
        Up,
        Nearest
    }

    public enum ExchangeTransactionType
    {
        Ready,
        Buy,
        Sell
    }

    public enum PayoutMethodType
    {
        Cash,
        TransitCard,
        PrepaidCard
    }

    public sealed class CommerceTransaction
    {
        public TransactionInfo Info { get; set; } = new();
        public ServiceType ServiceType { get; set; }
        public CardFeature SelectedCardFeatures { get; set; } = CardFeature.None;

        public FundingInfo Funding { get; set; } = new();
        public ComplianceInfo Compliance { get; set; } = new();

        public ExchangeRateInfo? ExchangeRate { get; set; }
        public ExchangePolicyInfo? ExchangePolicy { get; set; }
        public ConversionInfo? Conversion { get; set; }

        public AllocationPlan Allocation { get; set; } = new();
        public FulfillmentResult Fulfillment { get; set; } = new();
    }

    public sealed class TransactionInfo
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTimeOffset TransactionTime { get; set; } = DateTimeOffset.UtcNow;
        public string KioskId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string LocalCurrency { get; set; } = string.Empty;
    }

    public sealed class FundingInfo
    {
        public FundingType Type { get; set; }
        public string DepositCurrency { get; set; } = string.Empty;
        public List<DepositItem> DepositItems { get; set; } = new();
        public decimal RequestedPayAmount { get; set; }
        public string? EasyPayProvider { get; set; }

        public decimal DepositTotalAmount => DepositItems.Sum(x => x.Amount);
    }

    public sealed class DepositItem
    {
        public decimal Denomination { get; set; }
        public int Count { get; set; }
        public decimal Amount => Denomination * Count;
    }

    public sealed class ComplianceInfo
    {
        public bool RequiresIdentity { get; set; }
        public bool RequiresLimitCheck { get; set; }
        public CustomerIdentity? Customer { get; set; }
        public ExchangeLimitInfo? Limit { get; set; }
    }

    public sealed class CustomerIdentity
    {
        public string IdType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
    }

    public sealed class ExchangeLimitInfo
    {
        public bool IsChecked { get; set; }
        public bool IsApproved { get; set; }
        public decimal DailyLimitAmount { get; set; }
        public decimal DailyRemainingAmount { get; set; }
        public decimal PerTransactionLimitAmount { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset? CheckedAt { get; set; }

        public decimal AppliedMaxAmount => Math.Min(DailyRemainingAmount, PerTransactionLimitAmount);
    }

    public sealed class ExchangeRateInfo
    {
        public string SourceCurrency { get; set; } = string.Empty;
        public string TargetCurrency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }

    public sealed class ExchangePolicyInfo
    {
        public decimal FeePercent { get; set; }
        public decimal FeeFlat { get; set; }
        public decimal RoundingUnit { get; set; } = 1m;
        public TransactionRoundingMode RoundingMode { get; set; } = TransactionRoundingMode.Down;
    }

    public sealed class ConversionInfo
    {
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
    }

    public sealed class AllocationPlan
    {
        public List<AllocationItem> Items { get; set; } = new();

        public decimal CashAmount => Items.Where(x => x.Channel == AllocationChannel.Cash).Sum(x => x.Amount);
        public decimal TransitAmount => Items.Where(x => x.Channel == AllocationChannel.Transit).Sum(x => x.Amount);
        public decimal PrepaidAmount => Items.Where(x => x.Channel == AllocationChannel.Prepaid).Sum(x => x.Amount);
        public decimal TotalAmount => Items.Sum(x => x.Amount);
    }

    public sealed class AllocationItem
    {
        public AllocationChannel Channel { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class FulfillmentResult
    {
        public CashResult Cash { get; set; } = new();
        public CardResult Card { get; set; } = new();
        public bool IsSuccess { get; set; }
        public string? ErrorCode { get; set; }
    }

    public sealed class CashResult
    {
        public List<CashResultItem> Details { get; set; } = new();
        public string? ErrorCode { get; set; }
    }

    public sealed class CashResultItem
    {
        public string DeviceId { get; set; } = string.Empty;
        public int Slot { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal Denomination { get; set; }
        public int PlannedCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int RejectedCount { get; set; }
    }

    public sealed class CardResult
    {
        public bool IsPurchased { get; set; }
        public bool IsDispensed { get; set; }
        public WalletResult? Prepaid { get; set; }
        public WalletResult? Transit { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorCode { get; set; }
    }

    public sealed class WalletResult
    {
        public string CardId { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal PlannedTopupAmount { get; set; }
        public decimal CompletedTopupAmount { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorCode { get; set; }
    }
}
