namespace Kiosk.Application.Services.Exchange;

public sealed record CashBalanceSlot(
    string Device,
    int Slot,
    string Currency,
    decimal Denomination,
    int Count)
{
    public decimal TotalAmount => Denomination * Count;
}

public sealed record CurrencyBalanceSummary(
    string Currency,
    decimal TotalAmount,
    IReadOnlyList<CashBalanceSlot> Slots);

public sealed record DepositLimitSnapshot(
    string Currency,
    decimal DailyMaximumAmount,
    decimal DailyRemainingMaximumAmount,
    decimal PerTransactionMaximumAmount);
