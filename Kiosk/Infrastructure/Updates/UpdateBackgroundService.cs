using Kiosk.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Kiosk.Infrastructure.Updates;

public sealed class UpdateBackgroundService : BackgroundService
{
    private static readonly TimeSpan LoopInterval = TimeSpan.FromSeconds(15);

    private readonly IAppUpdateService _appUpdateService;
    private readonly VelopackOptions _options;
    private readonly ILoggingService _loggingService;

    public UpdateBackgroundService(
        IAppUpdateService appUpdateService,
        VelopackOptions options,
        ILoggingService loggingService)
    {
        _appUpdateService = appUpdateService;
        _options = options;
        _loggingService = loggingService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsConfigured)
            return;

        _loggingService.Info("Velopack periodic update background service started.");

        using var timer = new PeriodicTimer(LoopInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await _appUpdateService.RunPeriodicWorkAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex, $"Velopack periodic background service failed. {ex.Message}");
        }
    }
}
