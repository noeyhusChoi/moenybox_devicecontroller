using System.Globalization;
using System.IO;
using Kiosk.Application.Services.Devices.Withdrawal;
using Kiosk.Application.Services.Exchange;

namespace Kiosk.ViewModels.Steps;

internal static class DispenseResultViewModelSupport
{
    public static IReadOnlyList<DispenseBreakdownRowViewModel> BuildBreakdownRows(
        string currencyCode,
        IReadOnlyList<WithdrawalAllocation> allocations,
        IReadOnlyList<CashBalanceSlot> balanceSlots)
    {
        var denominations = balanceSlots
            .Where(x => string.Equals(x.Currency, currencyCode, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Denomination)
            .Distinct()
            .OrderByDescending(x => x)
            .ToArray();

        var allocationMap = allocations
            .Where(x => string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.Denomination)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

        var rows = new List<DispenseBreakdownRowViewModel>();
        foreach (var denomination in denominations)
        {
            allocationMap.TryGetValue(denomination, out var count);
            rows.Add(new DispenseBreakdownRowViewModel(
                denomination.ToString("#,0", CultureInfo.InvariantCulture),
                count));
        }

        return rows;
    }

    public static string NormalizeCurrency(string? currency, string fallback)
        => string.IsNullOrWhiteSpace(currency) ? fallback : currency.ToUpperInvariant();

    public static string? CreateFlagPath(string currencyCode)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "Assets", "Flag", $"{currencyCode}.png");
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        return null;
    }
}

public sealed record DispenseBreakdownRowViewModel(
    string DenominationText,
    int Count);
