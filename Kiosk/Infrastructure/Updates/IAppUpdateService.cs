namespace Kiosk.Infrastructure.Updates;

public interface IAppUpdateService
{
    Task CheckAndApplyOnStartupAsync(CancellationToken cancellationToken = default);
    Task RunPeriodicWorkAsync(CancellationToken cancellationToken = default);
    void NotifyUserInteraction();
    void SetMainIdleState(bool isIdle);
}
