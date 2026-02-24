using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers;

public interface IWithdrawalDriver : IDevice
{
    Task<CommandResult> DispenseAsync(byte[] payload, CancellationToken ct = default);
}
