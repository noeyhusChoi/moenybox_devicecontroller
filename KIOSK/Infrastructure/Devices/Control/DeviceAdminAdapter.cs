using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KIOSK.Infrastructure.Devices.Control
{
    public sealed class DeviceAdminAdapter : IDeviceAdminPort
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IDeviceCommandCatalog _commandCatalog;

        public DeviceAdminAdapter(
            IDeviceManager deviceManager,
            IDeviceCommandCatalog commandCatalog)
        {
            _deviceManager = deviceManager;
            _commandCatalog = commandCatalog;
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

        public IReadOnlyCollection<DeviceCommandInfo> GetFor(string deviceId)
            => _commandCatalog.GetFor(deviceId)
                .Select(x => new DeviceCommandInfo(x.Name, x.Description))
                .ToArray();

        public IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandInfo>> GetAll()
            => _commandCatalog.GetAll()
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyCollection<DeviceCommandInfo>)x.Value
                        .Select(c => new DeviceCommandInfo(c.Name, c.Description))
                        .ToArray(),
                    StringComparer.OrdinalIgnoreCase);
    }
}
