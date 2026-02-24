using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers;

public interface IPrinterDriver : IDevice
{
    Task<CommandResult> PrintTitleAsync(string content, CancellationToken ct = default);
    Task<CommandResult> PrintContentAsync(string content, CancellationToken ct = default);
    Task<CommandResult> CutAsync(CancellationToken ct = default);
}
