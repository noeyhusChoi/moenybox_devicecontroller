using System.Threading;
using System.Threading.Tasks;
using DeviceKit.Drivers.Withdrawal;

namespace DeviceKit.Drivers;

internal interface IWithdrawalDriver : IDeviceDriver
{
    Task<DeviceCommandResponse> ReadSensorsAsync(CancellationToken ct = default);
    Task<DeviceCommandResponse> InitializeHardwareAsync(CancellationToken ct = default);
    Task<DeviceCommandResponse> EjectAsync(WithdrawalEjectRequest request, CancellationToken ct = default);
    Task<DeviceCommandResponse> DispenseAsync(IReadOnlyList<WithdrawalDispenseSlotRequest> requests, CancellationToken ct = default);
}
