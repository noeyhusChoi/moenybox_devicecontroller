using KIOSK.Device.Abstractions;

namespace KIOSK.Application.Services.Devices
{
    public interface IDeviceAdminPort
    {
        Task<CommandResult> SendAsync(
            string deviceId,
            DeviceCommand command,
            CancellationToken ct = default);

        Task<CommandResult> SendAsync(
            string deviceId,
            DeviceCommand command,
            CommandContext context,
            CancellationToken ct = default);

        IReadOnlyCollection<DeviceCommandInfo> GetFor(string deviceId);
        IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandInfo>> GetAll();
    }
}
