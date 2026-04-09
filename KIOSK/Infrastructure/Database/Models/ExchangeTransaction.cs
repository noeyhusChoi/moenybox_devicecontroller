using Kiosk.Domain.Entities;

namespace Kiosk.Infrastructure.Database.Models
{
    public enum ExchangeTransactionType
    {
        Ready,
        Buy,
        Sell
    }

    // 거래 정보
    public sealed class TransactionInfo
    {
        public DateTime TransactionTime { get; set; } = DateTime.Now;
        public string TransactionId { get; set; } = string.Empty;
        public ExchangeTransactionType TransactionType { get; set; } = ExchangeTransactionType.Ready;
    }

    // 환율 정보
    public sealed class ExchangeRateInfo
    {
        public string SourceCurrency { get; set; } = string.Empty;
        public string TargetCurrency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }

    // 정책
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

    // 거래 한도
    public sealed class ExchangeLimitInfo
    {
        public bool IsChecked { get; set; }
        public bool IsApproved { get; set; }
        public decimal DailyLimitAmount { get; set; }
        public decimal DailyRemainingAmount { get; set; }
        public decimal PerTransactionLimitAmount { get; set; }
        public decimal AppliedMaxAmount => Math.Min(DailyRemainingAmount, PerTransactionLimitAmount);
        public string ReasonCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime? CheckedAt { get; set; }
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

    #region Temp
    public enum PayoutMethodType
    {
        Cash,
        TransitCard,
        PrepaidCard
    }

    public enum PayoutLineStatus
    {
        Succeeded,
        Failed,
        Partial
    }

    public sealed class CashPayoutItem
    {
        public decimal Denomination { get; set; }
        public int Count { get; set; }
        public decimal Amount => Denomination * Count;
    }

    // 권종별 실행 결과(요청/성공/실패/리젝트)를 함께 보관한다.
    public sealed class CashPayoutExecutionItem
    {
        public string DeviceId { get; set; } = string.Empty;
        public int Slot { get; set; }
        public decimal Denomination { get; set; }
        public int RequestedCount { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public int RejectedCount { get; set; }
        public decimal RequestedAmount => Denomination * RequestedCount;
        public decimal SucceededAmount => Denomination * SucceededCount;
        public decimal FailedAmount => Denomination * FailedCount;
    }

    public interface IPayoutDetail { }

    public sealed class NoPayoutDetail : IPayoutDetail { }

    public sealed class CashPayoutDetail
        : IPayoutDetail
    {
        public List<CashPayoutItem> Items { get; } = new();
        public List<CashPayoutExecutionItem> Executions { get; } = new();
    }

    public sealed class TransitCardTopupDetail
        : IPayoutDetail
    {
        public string CardId { get; set; } = string.Empty;
        public string ApprovalNo { get; set; } = string.Empty;
        public decimal BalanceAfter { get; set; }
    }

    public sealed class PrepaidCardTopupDetail
        : IPayoutDetail
    {
        public string CardId { get; set; } = string.Empty;
        public string ApprovalNo { get; set; } = string.Empty;
        public decimal BalanceAfter { get; set; }
    }

    public sealed class PayoutMethodInfo
    {
        public PayoutMethodType Method { get; set; }
        public PayoutLineStatus Status { get; set; } = PayoutLineStatus.Succeeded;
        public decimal RequestedAmount { get; set; }
        public decimal PlannedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal FailedAmount { get; set; }
        public string FailureCode { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public IPayoutDetail Detail { get; set; } = new NoPayoutDetail();
        public CashPayoutDetail? CashDetail { get; set; }
        public TransitCardTopupDetail? TransitCardDetail { get; set; }
        public PrepaidCardTopupDetail? PrepaidCardDetail { get; set; }
    }

    public sealed class PayoutInfo
    {
        public decimal PlannedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal FailedAmount { get; set; }
        public decimal RejectedAmount { get; set; }
        public List<PayoutMethodInfo> Methods { get; } = new();
    }
    #endregion

    public sealed class ExchangeTransaction
    {
        public TransactionInfo Info { get; set; } = new();
        public CustomerInfo Customer { get; set; } = new();
        public DepositInfo Deposit { get; set; } = new();
        public PayoutInfo Payout { get; set; } = new();
        public ExchangeRateInfo Rate { get; set; } = new();
        public ExchangePolicyInfo Policy { get; set; } = new();
        public ExchangeLimitInfo Limit { get; set; } = new();
        public decimal? TargetRequestedAmount { get; set; }
        public decimal SourceRequiredAmount { get; set; }
        public decimal SourceChangeAmount { get; set; }
        public decimal TargetMinorRemainderAmount { get; set; }
        public decimal ChangeMinorRemainderAmount { get; set; }
    }
}
