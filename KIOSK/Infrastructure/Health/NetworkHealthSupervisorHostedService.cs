using System.Net.NetworkInformation;
using KIOSK.Device.Abstractions;
using Microsoft.Extensions.Hosting;

namespace KIOSK.Infrastructure.Health;

public sealed class NetworkHealthSupervisorHostedService : BackgroundService
{
    private readonly IHealthPipeline _healthPipeline;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public NetworkHealthSupervisorHostedService(IHealthPipeline healthPipeline)
    {
        _healthPipeline = healthPipeline;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            PublishNetworkHealth();

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

    private void PublishNetworkHealth()
    {
        var isAvailable = NetworkInterface.GetIsNetworkAvailable();
        var alerts = new List<StatusEvent>();

        if (!isAvailable)
        {
            alerts.Add(new StatusEvent(
                Code: "SYS.NETWORK.STATUS.OFFLINE",
                Message: string.Empty,
                Severity: Severity.Error,
                At: DateTimeOffset.UtcNow,
                Source: AlertSource.System));
        }

        var snapshot = new StatusSnapshot
        {
            Name = SystemHealthSourceIds.Network,
            Model = "SYSTEM",
            Health = isAvailable ? DeviceHealth.Online : DeviceHealth.Offline,
            Timestamp = DateTimeOffset.UtcNow,
            AlertScope = AlertSource.System,
            Alerts = alerts
        };

        _healthPipeline.Process(new HealthSignal(HealthSourceKind.Network, SystemHealthSourceIds.Network, snapshot));
    }
}
