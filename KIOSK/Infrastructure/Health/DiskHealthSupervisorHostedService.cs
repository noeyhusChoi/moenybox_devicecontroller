using System.IO;
using KIOSK.Device.Abstractions;
using Microsoft.Extensions.Hosting;

namespace KIOSK.Infrastructure.Health;

public sealed class DiskHealthSupervisorHostedService : BackgroundService
{
    private readonly IHealthPipeline _healthPipeline;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private readonly long _diskFreeThresholdBytes = 512L * 1024L * 1024L; // 512MB

    public DiskHealthSupervisorHostedService(IHealthPipeline healthPipeline)
    {
        _healthPipeline = healthPipeline;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            PublishDiskHealth();

            try
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void PublishDiskHealth()
    {
        var root = Path.GetPathRoot(AppContext.BaseDirectory) ?? "C:\\";
        var alerts = new List<StatusEvent>();
        var isHealthy = true;

        try
        {
            var drive = new DriveInfo(root);
            if (!drive.IsReady)
            {
                isHealthy = false;
                alerts.Add(new StatusEvent(
                    Code: "SYS.DISK.STATUS.UNAVAILABLE",
                    Message: string.Empty,
                    Severity: Severity.Error,
                    At: DateTimeOffset.UtcNow,
                    Source: AlertSource.System));
            }
            else if (drive.AvailableFreeSpace < _diskFreeThresholdBytes)
            {
                isHealthy = false;
                alerts.Add(new StatusEvent(
                    Code: "SYS.DISK.STATUS.LOW_SPACE",
                    Message: string.Empty,
                    Severity: Severity.Warning,
                    At: DateTimeOffset.UtcNow,
                    Source: AlertSource.System));
            }
        }
        catch
        {
            isHealthy = false;
            alerts.Add(new StatusEvent(
                Code: "SYS.DISK.STATUS.ERROR",
                Message: string.Empty,
                Severity: Severity.Error,
                At: DateTimeOffset.UtcNow,
                Source: AlertSource.System));
        }

        var snapshot = new StatusSnapshot
        {
            Name = SystemHealthSourceIds.Disk,
            Model = "SYSTEM",
            Health = isHealthy ? DeviceHealth.Online : DeviceHealth.Offline,
            Timestamp = DateTimeOffset.UtcNow,
            AlertScope = AlertSource.System,
            Alerts = alerts
        };

        _healthPipeline.Process(new HealthSignal(HealthSourceKind.Disk, SystemHealthSourceIds.Disk, snapshot));
    }
}
