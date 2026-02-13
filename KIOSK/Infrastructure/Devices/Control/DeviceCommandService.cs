using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Control
{
    public sealed class DeviceCommandService : IDeviceCommandService
    {
        private readonly IDeviceManager _deviceManager;

        public DeviceCommandService(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public Task<CommandResult> SendAsync(
            string deviceId,
            DeviceCommand command,
            CancellationToken ct = default)
            => _deviceManager.SendAsync(deviceId, command, ct);

        public Task<CommandResult> SendAsync(
            string deviceId,
            DeviceCommand command,
            CommandContext context,
            CancellationToken ct = default)
            => _deviceManager.SendAsync(deviceId, command, context, ct);
    }
}
