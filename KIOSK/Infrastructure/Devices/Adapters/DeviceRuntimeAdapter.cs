using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.DeviceCommon.Devices;
using KIOSK.Infrastructure.Devices.Runtime;
using KIOSK.Infrastructure.Devices.Status;

namespace KIOSK.Infrastructure.Devices.Adapters
{
    public sealed class DeviceRuntimeAdapter : IDeviceRuntimePort
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IStatusStore _statusStore;

        public DeviceRuntimeAdapter(IDeviceManager deviceManager, IStatusStore statusStore)
        {
            _deviceManager = deviceManager;
            _statusStore = statusStore;
        }

        public Task<IReadOnlyList<DeviceStatusSnapshot>> GetStatusesAsync(CancellationToken cancellationToken = default)
        {
            var snapshots = _deviceManager.GetAllDevices()
                .Select(MapStatus)
                .OrderBy(x => x.DeviceId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult((IReadOnlyList<DeviceStatusSnapshot>)snapshots);
        }

        public Task<DeviceStatusSnapshot?> GetStatusAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (!_deviceManager.TryGetDevice(deviceId, out var info))
                return Task.FromResult<DeviceStatusSnapshot?>(null);

            return Task.FromResult<DeviceStatusSnapshot?>(MapStatus(info));
        }

        public Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
            => _deviceManager.ConnectAsync(deviceId, cancellationToken);

        public Task<bool> DisconnectAsync(string deviceId, CancellationToken cancellationToken = default)
            => _deviceManager.DisconnectAsync(deviceId, cancellationToken);

        public async Task<DeviceCommandResult> ExecuteAsync(
            string deviceId,
            DeviceCommandRequest command,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
                return new DeviceCommandResult(false, "INVALID_COMMAND", "Command name is required.");

            var result = await _deviceManager
                .SendAsync(deviceId, new DeviceCommand(command.Name, command.Payload), cancellationToken)
                .ConfigureAwait(false);

            return MapCommandResult(result);
        }

        private DeviceStatusSnapshot MapStatus(DeviceRuntimeInfo info)
        {
            var snapshot = _statusStore.TryGet(info.DeviceId);
            var connectionState = ToConnectionState(snapshot);
            var isHealthy = connectionState == DeviceConnectionState.Connected;
            var message = GetStatusMessage(snapshot);
            var timestamp = snapshot?.Timestamp ?? DateTimeOffset.UtcNow;

            return new DeviceStatusSnapshot(
                info.DeviceId,
                info.DeviceType,
                connectionState,
                isHealthy,
                message,
                timestamp);
        }

        private static DeviceConnectionState ToConnectionState(StatusSnapshot? snapshot)
        {
            if (snapshot is null || snapshot.Health == DeviceHealth.Offline)
                return DeviceConnectionState.Disconnected;

            if (snapshot.Alerts?.Any(a => a.Severity is Severity.Error or Severity.Critical) == true)
                return DeviceConnectionState.Faulted;

            return DeviceConnectionState.Connected;
        }

        private static string GetStatusMessage(StatusSnapshot? snapshot)
        {
            if (snapshot?.Alerts is not { Count: > 0 })
                return string.Empty;

            var latest = snapshot.Alerts[^1];
            if (!string.IsNullOrWhiteSpace(latest.Message))
                return latest.Message;

            return latest.Code ?? string.Empty;
        }

        private static DeviceCommandResult MapCommandResult(CommandResult result)
        {
            var code = result.Code?.ToString() ?? (result.Success ? "OK" : "FAILED");
            var message = result.Message ?? string.Empty;
            var rawResponse = result.Data?.ToString();
            return new DeviceCommandResult(result.Success, code, message, rawResponse);
        }
    }
}
