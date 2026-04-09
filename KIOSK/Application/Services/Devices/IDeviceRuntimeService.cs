using DeviceKit.Engine;

namespace Kiosk.Application.Services.Devices;

public interface IDeviceRuntimeService
{
    Task<IDeviceManagerPort> GetPortAsync(CancellationToken ct = default);
}
