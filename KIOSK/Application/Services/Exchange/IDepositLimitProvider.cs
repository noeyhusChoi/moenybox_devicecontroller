namespace Kiosk.Application.Services.Exchange;

public interface IDepositLimitProvider
{
    Task<DepositLimitSnapshot> GetDepositLimitAsync(string currency, CancellationToken ct = default);
}
