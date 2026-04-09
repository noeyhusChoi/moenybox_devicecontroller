namespace Kiosk.Application.Services.Exchange;

public sealed class FixedDepositLimitProvider : IDepositLimitProvider
{
    private static readonly DepositLimitSnapshot DefaultLimit =
        new("fx", 588_000m, 441_000m, 588_000m);

    public Task<DepositLimitSnapshot> GetDepositLimitAsync(string currency, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        return Task.FromResult(DefaultLimit with { Currency = currency.ToLowerInvariant() });
    }
}
