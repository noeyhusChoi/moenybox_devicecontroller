using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Device.Abstractions;

public interface IDeviceCommandHandler
{
    string Name { get; }
    Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct);
}
