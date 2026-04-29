namespace Kiosk.Application.Services.Health
{
    public interface IDatabaseHealthService
    {
        Task<bool> CanConnectAsync(CancellationToken ct = default);
    }
}
