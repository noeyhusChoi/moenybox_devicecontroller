using System.Collections.Generic;
using System.Linq;
using KIOSK.Device.Abstractions;
using KIOSK.DeviceCommon.Devices;
using KIOSK.Infrastructure.Devices.Runtime.Factories;

namespace KIOSK.DeviceRuntime.Ports;

public sealed class SharedDeviceRuntimePort : IDeviceRuntimePort, IAsyncDisposable
{
    private readonly ITransportFactory _transportFactory;
    private readonly IDeviceFactory _deviceFactory;
    private readonly Dictionary<string, DeviceSlot> _slots;

    public SharedDeviceRuntimePort(
        IEnumerable<DeviceDescriptor> descriptors,
        ITransportFactory? transportFactory = null,
        IDeviceFactory? deviceFactory = null)
    {
        _transportFactory = transportFactory ?? new TransportFactory();
        _deviceFactory = deviceFactory ?? new DeviceFactory();
        _slots = descriptors
            .Where(d => d.Validate)
            .ToDictionary(
                d => d.EffectiveId,
                d => new DeviceSlot(d, CreateDisconnectedSnapshot(d, "Disconnected")),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<DeviceStatusSnapshot>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<DeviceStatusSnapshot>(_slots.Count);
        foreach (var slot in _slots.Values.OrderBy(x => x.Descriptor.EffectiveId, StringComparer.OrdinalIgnoreCase))
            result.Add(await GetStatusInternalAsync(slot, cancellationToken).ConfigureAwait(false));
        return result;
    }

    public async Task<DeviceStatusSnapshot?> GetStatusAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return null;

        return await GetStatusInternalAsync(slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return false;

        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (slot.Device is not null)
                return false;

            ITransport? transport = null;
            IDevice? device = null;

            try
            {
                transport = _transportFactory.Create(slot.Descriptor);
                await transport.OpenAsync(cancellationToken).ConfigureAwait(false);

                device = _deviceFactory.Create(slot.Descriptor, transport);
                var init = await device.InitializeAsync(cancellationToken).ConfigureAwait(false);

                slot.Transport = transport;
                slot.Device = device;
                slot.Snapshot = MapSnapshot(slot.Descriptor, init, true, "Connected");
                return true;
            }
            catch (Exception ex)
            {
                await SafeDisposeAsync(device, transport, cancellationToken).ConfigureAwait(false);
                slot.Snapshot = CreateDisconnectedSnapshot(slot.Descriptor, ex.Message);
                return false;
            }
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    public async Task<bool> DisconnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return false;

        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (slot.Device is null && slot.Transport is null)
                return false;

            await DisposeSessionAsync(slot, cancellationToken).ConfigureAwait(false);
            slot.Snapshot = CreateDisconnectedSnapshot(slot.Descriptor, "Disconnected");
            return true;
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    public async Task<DeviceCommandResult> ExecuteAsync(
        string deviceId,
        DeviceCommandRequest command,
        CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return new DeviceCommandResult(false, "NOT_FOUND", "Device not found.");

        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (slot.Device is null)
                return new DeviceCommandResult(false, "NOT_CONNECTED", "Device is not connected.");

            try
            {
                var result = await slot.Device
                    .ExecuteAsync(new DeviceCommand(command.Name, command.Payload), cancellationToken)
                    .ConfigureAwait(false);

                var code = result.Code?.ToString() ?? (result.Success ? "OK" : "ERROR");
                var message = string.IsNullOrWhiteSpace(result.Message) ? code : result.Message;
                return new DeviceCommandResult(result.Success, code, message, result.Data?.ToString());
            }
            catch (Exception ex)
            {
                await DisposeSessionAsync(slot, cancellationToken).ConfigureAwait(false);
                slot.Snapshot = CreateDisconnectedSnapshot(slot.Descriptor, ex.Message);
                return new DeviceCommandResult(false, "EXECUTION_ERROR", ex.Message);
            }
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var slot in _slots.Values)
        {
            await slot.Gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await DisposeSessionAsync(slot, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                slot.Gate.Release();
                slot.Gate.Dispose();
            }
        }
    }

    private async Task<DeviceStatusSnapshot> GetStatusInternalAsync(DeviceSlot slot, CancellationToken cancellationToken)
    {
        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (slot.Device is null)
                return slot.Snapshot;

            try
            {
                var status = await slot.Device.GetStatusAsync(cancellationToken).ConfigureAwait(false);
                slot.Snapshot = MapSnapshot(slot.Descriptor, status, true, slot.Snapshot.Message);
                return slot.Snapshot;
            }
            catch (Exception ex)
            {
                await DisposeSessionAsync(slot, cancellationToken).ConfigureAwait(false);
                slot.Snapshot = CreateDisconnectedSnapshot(slot.Descriptor, ex.Message);
                return slot.Snapshot;
            }
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    private static DeviceStatusSnapshot MapSnapshot(
        DeviceDescriptor descriptor,
        StatusSnapshot status,
        bool connected,
        string? fallbackMessage)
    {
        var hasError = status.Alerts?.Any(a => a.Severity is Severity.Error or Severity.Critical) == true;
        var state = connected
            ? (status.Health == DeviceHealth.Online ? DeviceConnectionState.Connected : DeviceConnectionState.Faulted)
            : DeviceConnectionState.Disconnected;
        var isHealthy = connected && status.Health == DeviceHealth.Online && !hasError;
        var message = ResolveMessage(status, fallbackMessage);

        return new DeviceStatusSnapshot(
            descriptor.EffectiveId,
            descriptor.DeviceType,
            state,
            isHealthy,
            message,
            status.Timestamp);
    }

    private static string ResolveMessage(StatusSnapshot status, string? fallbackMessage)
    {
        var alert = status.Alerts?
            .OrderByDescending(a => a.Severity)
            .FirstOrDefault();

        if (alert is null)
            return string.IsNullOrWhiteSpace(fallbackMessage) ? status.Health.ToString() : fallbackMessage;

        if (!string.IsNullOrWhiteSpace(alert.Message))
            return alert.Message;

        if (!string.IsNullOrWhiteSpace(alert.Code))
            return alert.Code;

        return string.IsNullOrWhiteSpace(fallbackMessage) ? status.Health.ToString() : fallbackMessage;
    }

    private static DeviceStatusSnapshot CreateDisconnectedSnapshot(DeviceDescriptor descriptor, string message)
        => new(
            descriptor.EffectiveId,
            descriptor.DeviceType,
            DeviceConnectionState.Disconnected,
            false,
            string.IsNullOrWhiteSpace(message) ? "Disconnected" : message,
            DateTimeOffset.UtcNow);

    private static async Task DisposeSessionAsync(DeviceSlot slot, CancellationToken cancellationToken)
    {
        await SafeDisposeAsync(slot.Device, slot.Transport, cancellationToken).ConfigureAwait(false);
        slot.Device = null;
        slot.Transport = null;
    }

    private static async Task SafeDisposeAsync(IDevice? device, ITransport? transport, CancellationToken cancellationToken)
    {
        try
        {
            if (transport is not null)
                await transport.CloseAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            if (device is IAsyncDisposable asyncDevice)
                await asyncDevice.DisposeAsync().ConfigureAwait(false);
            else if (device is IDisposable disposableDevice)
                disposableDevice.Dispose();
        }
        catch
        {
        }

        try
        {
            if (transport is not null)
                await transport.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private sealed class DeviceSlot
    {
        public DeviceSlot(DeviceDescriptor descriptor, DeviceStatusSnapshot snapshot)
        {
            Descriptor = descriptor;
            Snapshot = snapshot;
        }

        public DeviceDescriptor Descriptor { get; }
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public ITransport? Transport { get; set; }
        public IDevice? Device { get; set; }
        public DeviceStatusSnapshot Snapshot { get; set; }
    }
}
