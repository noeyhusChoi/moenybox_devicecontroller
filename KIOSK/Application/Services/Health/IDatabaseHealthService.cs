namespace KIOSK.Application.Services.Health
{
    public interface IDatabaseHealthService
    {
        Task<bool> CanConnectAsync(CancellationToken ct = default);
    }
}
