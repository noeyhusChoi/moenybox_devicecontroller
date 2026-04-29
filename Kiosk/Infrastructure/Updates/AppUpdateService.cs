using Kiosk.Application.Abstractions;
using Velopack;
using Velopack.Sources;

namespace Kiosk.Infrastructure.Updates;

public sealed class AppUpdateService : IAppUpdateService, IDisposable
{
    private readonly VelopackOptions _options;
    private readonly ILoggingService _loggingService;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private readonly object _stateLock = new();
    private DateTimeOffset _lastUserInteractionAt = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastPeriodicCheckAt = DateTimeOffset.MinValue;
    private VelopackAsset? _pendingPeriodicUpdate;
    private bool _isApplyingUpdate;
    private bool _isMainIdleState;

    public AppUpdateService(VelopackOptions options, ILoggingService loggingService)
    {
        _options = options;
        _loggingService = loggingService;
    }

    public async Task CheckAndApplyOnStartupAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _loggingService.Info("Velopack startup check skipped. Feed URL is not configured.");
            return;
        }

        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var manager = CreateManager();
            if (!manager.IsInstalled)
            {
                _loggingService.Info("Velopack startup check skipped. Application is not running from an installed Velopack location.");
                return;
            }

            if (manager.UpdatePendingRestart is { } pendingRestart)
            {
                _loggingService.Info($"Velopack startup apply pending update: {pendingRestart.Version}");
                _isApplyingUpdate = true;
                manager.ApplyUpdatesAndRestart(pendingRestart);
                return;
            }

            _loggingService.Info("Velopack startup update check started.");
            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
            {
                _loggingService.Info("Velopack startup update check completed. No update available.");
                return;
            }

            _loggingService.Info($"Velopack startup update found: {update.TargetFullRelease.Version}. Downloading.");
            await manager.DownloadUpdatesAsync(update, null, cancellationToken);
            _loggingService.Info($"Velopack startup update downloaded: {update.TargetFullRelease.Version}. Applying immediately.");
            _isApplyingUpdate = true;
            manager.ApplyUpdatesAndRestart(update.TargetFullRelease);
        }
        catch (OperationCanceledException)
        {
            _loggingService.Warn("Velopack startup update check cancelled.");
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex, $"Velopack startup update check failed. {ex.Message}");
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task RunPeriodicWorkAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            return;

        if (ShouldRunPeriodicCheck())
        {
            await CheckAndStagePeriodicUpdateAsync(cancellationToken);
        }

        await TryApplyPendingPeriodicUpdateAsync(cancellationToken);
    }

    public void NotifyUserInteraction()
    {
        lock (_stateLock)
        {
            _lastUserInteractionAt = DateTimeOffset.UtcNow;
        }
    }

    public void SetMainIdleState(bool isIdle)
    {
        lock (_stateLock)
        {
            _isMainIdleState = isIdle;
            if (isIdle)
            {
                _lastUserInteractionAt = DateTimeOffset.UtcNow;
            }
        }
    }

    public void Dispose()
    {
        _operationLock.Dispose();
    }

    private bool ShouldRunPeriodicCheck()
    {
        lock (_stateLock)
        {
            return !_isApplyingUpdate && DateTimeOffset.UtcNow - _lastPeriodicCheckAt >= _options.PeriodicCheckInterval;
        }
    }

    private async Task CheckAndStagePeriodicUpdateAsync(CancellationToken cancellationToken)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var manager = CreateManager();
            if (!manager.IsInstalled)
                return;

            _lastPeriodicCheckAt = DateTimeOffset.UtcNow;

            if (manager.UpdatePendingRestart is { } pendingRestart)
            {
                _pendingPeriodicUpdate = pendingRestart;
                _loggingService.Info($"Velopack pending update already downloaded: {pendingRestart.Version}");
                return;
            }

            _loggingService.Info("Velopack periodic update check started.");
            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
            {
                _loggingService.Info("Velopack periodic update check completed. No update available.");
                return;
            }

            _loggingService.Info($"Velopack periodic update found: {update.TargetFullRelease.Version}. Downloading.");
            await manager.DownloadUpdatesAsync(update, null, cancellationToken);
            _pendingPeriodicUpdate = update.TargetFullRelease;
            _loggingService.Info($"Velopack periodic update downloaded and waiting for idle apply: {update.TargetFullRelease.Version}");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex, $"Velopack periodic update check failed. {ex.Message}");
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private async Task TryApplyPendingPeriodicUpdateAsync(CancellationToken cancellationToken)
    {
        VelopackAsset? pendingUpdate;

        lock (_stateLock)
        {
            if (_isApplyingUpdate || !_isMainIdleState || _pendingPeriodicUpdate is null)
                return;

            if (DateTimeOffset.UtcNow - _lastUserInteractionAt < _options.IdleApplyThreshold)
                return;

            _isApplyingUpdate = true;
            pendingUpdate = _pendingPeriodicUpdate;
        }

        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var manager = CreateManager();
            if (!manager.IsInstalled)
                return;

            _loggingService.Info($"Velopack applying pending periodic update: {pendingUpdate!.Version}");
            manager.WaitExitThenApplyUpdates(pendingUpdate, restart: true);

            if (System.Windows.Application.Current is not null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => System.Windows.Application.Current.Shutdown());
            }
        }
        catch (OperationCanceledException)
        {
            lock (_stateLock)
            {
                _isApplyingUpdate = false;
            }
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _isApplyingUpdate = false;
            }

            _loggingService.Error(ex, $"Velopack periodic update apply failed. {ex.Message}");
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private UpdateManager CreateManager()
    {
        var updateOptions = new UpdateOptions();
        if (!string.IsNullOrWhiteSpace(_options.ExplicitChannel))
        {
            updateOptions.ExplicitChannel = _options.ExplicitChannel;
        }

        var source = new GithubSource(_options.FeedUrl!, string.Empty, prerelease: false);
        return new UpdateManager(source, updateOptions);
    }
}
