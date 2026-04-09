namespace Kiosk.Application.Services.Exchange;

public sealed class FixedExchangeCashBalanceProvider : IExchangeCashBalanceProvider
{
    private static readonly IReadOnlyList<CashBalanceSlot> Slots =
    [
        new("HCDM3", 1, "krw", 50_000m, 10),
        new("HCDM3", 2, "krw", 10_000m, 10),
        new("HCDM3", 3, "krw", 5_000m, 100),
        new("HCDM3", 4, "krw", 1_000m, 10),
        new("HCDM2", 2, "usd", 50m, 10)
    ];

    public Task<IReadOnlyList<CashBalanceSlot>> GetSlotsAsync(CancellationToken ct = default)
        => Task.FromResult(Slots);
}
