using System.Diagnostics;
using KIOSK.Devices.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KIOSK.Devices.Management;

/// <summary>
/// appsettings.json에 정의된 DeviceDescriptor들을 런타임에 등록한다.
/// </summary>
internal sealed class DeviceBootstrapper : BackgroundService
{
    private readonly IOptions<DevicePlatformOptions> _options;
    private readonly IDeviceManager _deviceManager;

    public DeviceBootstrapper(IOptions<DevicePlatformOptions> options, IDeviceManager deviceManager)
    {
        _options = options;
        _deviceManager = deviceManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var devices = _options.Value.Devices ?? new();
        foreach (var d in devices)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                await _deviceManager.AddAsync(d, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try { Trace.WriteLine($"[DeviceBootstrapper] add failed: {d?.Name} / {ex.Message}"); } catch { }
            }
        }

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }
}

