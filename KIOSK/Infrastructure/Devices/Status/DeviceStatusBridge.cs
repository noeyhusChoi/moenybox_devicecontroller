using System.Diagnostics;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime;
using KIOSK.Infrastructure.Health;

namespace KIOSK.Infrastructure.Devices.Status;

/// <summary>
/// DeviceHost status event를 HealthPipeline으로 연결한다.
/// </summary>
public sealed class DeviceStatusBridge : IDisposable
{
    private readonly IDeviceManager _manager;
    private readonly IHealthPipeline _healthPipeline;
    private readonly Action<string, StatusSnapshot> _handler;
    private bool _disposed;

    public DeviceStatusBridge(IDeviceManager manager, IHealthPipeline healthPipeline)
    {
        _manager = manager;
        _healthPipeline = healthPipeline;
        _handler = OnStatusUpdated;
        _manager.StatusUpdated += _handler;
    }

    private void OnStatusUpdated(string deviceId, StatusSnapshot snapshot)
    {
        try
        {
            _healthPipeline.Process(HealthSignal.FromDevice(deviceId, snapshot));
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[DeviceStatusBridge] Process failed. device={deviceId} error={ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _manager.StatusUpdated -= _handler;
    }
}
