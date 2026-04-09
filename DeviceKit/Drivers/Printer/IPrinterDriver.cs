using System.Threading;
using System.Threading.Tasks;

namespace DeviceKit.Drivers;

internal interface IPrinterDriver : IDeviceDriver
{
    Task<DeviceCommandResponse> PrintTitleAsync(string content, CancellationToken ct = default);
    Task<DeviceCommandResponse> PrintContentAsync(string content, CancellationToken ct = default);
    Task<DeviceCommandResponse> CutAsync(CancellationToken ct = default);
}
