namespace Kiosk.Application.Services.Exchange;

public interface IExchangeCashBalanceProvider
{
    Task<IReadOnlyList<CashBalanceSlot>> GetSlotsAsync(CancellationToken ct = default);
}
