using KIOSK.Device.Abstractions;

namespace KIOSK.Application.Services.Devices
{
    public interface IDeviceCommandService
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
    }
}
