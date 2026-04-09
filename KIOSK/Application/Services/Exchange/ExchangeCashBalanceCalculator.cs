namespace Kiosk.Application.Services.Exchange;

public static class ExchangeCashBalanceCalculator
{
    public static decimal CalculateTotalAmountForCurrency(
        IEnumerable<CashBalanceSlot> slots,
        string currency)
    {
        ArgumentNullException.ThrowIfNull(slots);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return slots
            .Where(slot => string.Equals(slot.Currency, currency, StringComparison.OrdinalIgnoreCase))
            .Sum(slot => slot.TotalAmount);
    }

    public static IReadOnlyList<CurrencyBalanceSummary> SummarizeByCurrency(IEnumerable<CashBalanceSlot> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);

        return slots
            .GroupBy(slot => slot.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CurrencyBalanceSummary(
                group.Key,
                group.Sum(slot => slot.TotalAmount),
                group
                    .OrderBy(slot => slot.Device, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(slot => slot.Slot)
                    .ToArray()))
            .OrderBy(summary => summary.Currency, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
