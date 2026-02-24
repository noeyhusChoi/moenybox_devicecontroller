using System.Collections.Generic;
using System.Linq;
using KIOSK.Device.Abstractions;
using KIOSK.DeviceCommon.Devices;
using KIOSK.Infrastructure.Devices.Runtime.Factories;

namespace KIOSK.DeviceRuntime.Ports;

/// <summary>
/// Single-loop polling runtime for admin usage.
/// - One scheduler loop manages status polling for all connected devices.
/// - Per-device gate serializes connect/disconnect/execute/status operations.
/// </summary>
public sealed class ScheduledDeviceRuntimePort : IDeviceRuntimePort, IDeviceRuntimeStatusEvents, IAsyncDisposable
{
    private readonly ITransportFactory _transportFactory;
    private readonly IDeviceFactory _deviceFactory;
    private readonly Dictionary<string, DeviceSlot> _slots;
    private readonly ScheduledDeviceRuntimeOptions _options;
    private readonly CancellationTokenSource _schedulerCts = new();
    private readonly Task _schedulerTask;
    private readonly int _schedulerTickMs;

    public event Action<DeviceStatusSnapshot>? StatusChanged;

    public ScheduledDeviceRuntimePort(
        IEnumerable<DeviceDescriptor> descriptors,
        ScheduledDeviceRuntimeOptions? options = null,
        ITransportFactory? transportFactory = null,
        IDeviceFactory? deviceFactory = null)
    {
        _options = (options ?? ScheduledDeviceRuntimeOptions.Default).Normalize();
        _transportFactory = transportFactory ?? new TransportFactory();
        _deviceFactory = deviceFactory ?? new DeviceFactory();
        _slots = descriptors
            .Where(d => d.Validate)
            .ToDictionary(
                d => d.EffectiveId,
                d => new DeviceSlot(d, CreateDisconnectedSnapshot(d, "Disconnected")),
                StringComparer.OrdinalIgnoreCase);

        _schedulerTickMs = _options.SchedulerTickMs ?? CalculateSchedulerTickMs(_slots.Values.Select(x => x.Descriptor), _options);
        _schedulerTask = Task.Run(() => RunSchedulerAsync(_schedulerCts.Token));
    }

    public Task<IReadOnlyList<DeviceStatusSnapshot>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = _slots.Values
            .OrderBy(x => x.Descriptor.EffectiveId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Snapshot)
            .ToList();

        return Task.FromResult((IReadOnlyList<DeviceStatusSnapshot>)snapshots);
    }

    public Task<DeviceStatusSnapshot?> GetStatusAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return Task.FromResult<DeviceStatusSnapshot?>(null);

        return Task.FromResult<DeviceStatusSnapshot?>(slot.Snapshot);
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return false;

        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await TryConnectSlotCoreAsync(slot, cancellationToken).ConfigureAwait(false);
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
            slot.FailCount = 0;
            slot.NextPollAt = DateTimeOffset.MaxValue;
            UpdateSnapshot(slot, CreateDisconnectedSnapshot(slot.Descriptor, "Disconnected"));
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

                slot.NextPollAt = DateTimeOffset.UtcNow;
                var code = result.Code?.ToString() ?? (result.Success ? "OK" : "ERROR");
                var message = string.IsNullOrWhiteSpace(result.Message) ? code : result.Message;
                return new DeviceCommandResult(result.Success, code, message, result.Data?.ToString());
            }
            catch (Exception ex)
            {
                await DisposeSessionAsync(slot, cancellationToken).ConfigureAwait(false);
                slot.FailCount++;
                slot.NextPollAt = DateTimeOffset.UtcNow.AddMilliseconds(GetBackoffMs(slot));
                UpdateSnapshot(slot, CreateDisconnectedSnapshot(slot.Descriptor, ex.Message));
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
        _schedulerCts.Cancel();
        try
        {
            await _schedulerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _schedulerCts.Dispose();
        }

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

    private async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var dueSlots = _slots.Values
                .Where(x => x.NextPollAt <= now)
                .OrderBy(x => x.NextPollAt)
                .ToArray();

            foreach (var slot in dueSlots)
            {
                await PollSlotAsync(slot, cancellationToken).ConfigureAwait(false);
            }

            await Task.Delay(_schedulerTickMs, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PollSlotAsync(DeviceSlot slot, CancellationToken cancellationToken)
    {
        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (slot.Device is null)
            {
                await TryConnectSlotCoreAsync(slot, cancellationToken).ConfigureAwait(false);
                return;
            }

            try
            {
                var status = await slot.Device.GetStatusAsync(cancellationToken).ConfigureAwait(false);
                slot.FailCount = 0;
                slot.NextPollAt = DateTimeOffset.UtcNow.AddMilliseconds(GetPollingMs(slot.Descriptor, _options));
                UpdateSnapshot(slot, MapSnapshot(slot.Descriptor, status, true, slot.Snapshot.Message));
            }
            catch (Exception ex)
            {
                slot.FailCount++;
                slot.NextPollAt = DateTimeOffset.UtcNow.AddMilliseconds(GetBackoffMs(slot));
                await DisposeSessionAsync(slot, cancellationToken).ConfigureAwait(false);
                UpdateSnapshot(slot, CreateDisconnectedSnapshot(slot.Descriptor, ex.Message));
            }
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    private async Task<bool> TryConnectSlotCoreAsync(DeviceSlot slot, CancellationToken cancellationToken)
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
            slot.FailCount = 0;
            slot.NextPollAt = DateTimeOffset.UtcNow;
            UpdateSnapshot(slot, MapSnapshot(slot.Descriptor, init, true, "Connected"));
            return true;
        }
        catch (Exception ex)
        {
            await SafeDisposeAsync(device, transport, cancellationToken).ConfigureAwait(false);
            slot.FailCount++;
            slot.NextPollAt = DateTimeOffset.UtcNow.AddMilliseconds(GetBackoffMs(slot));
            UpdateSnapshot(slot, CreateDisconnectedSnapshot(slot.Descriptor, ex.Message));
            return false;
        }
    }

    private void UpdateSnapshot(DeviceSlot slot, DeviceStatusSnapshot next)
    {
        if (slot.Snapshot == next)
            return;

        slot.Snapshot = next;
        try
        {
            StatusChanged?.Invoke(next);
        }
        catch
        {
        }
    }

    private static int CalculateSchedulerTickMs(IEnumerable<DeviceDescriptor> descriptors, ScheduledDeviceRuntimeOptions options)
    {
        var minPolling = descriptors
            .Select(d => GetPollingMs(d, options))
            .DefaultIfEmpty(options.DefaultPollingMs)
            .Min();

        return Math.Clamp(minPolling / 2, 250, 2000);
    }

    private static int GetPollingMs(DeviceDescriptor descriptor, ScheduledDeviceRuntimeOptions options)
        => Math.Max(options.MinPollingMs, descriptor.PollingMs > 0 ? descriptor.PollingMs : options.DefaultPollingMs);

    private int GetBackoffMs(DeviceSlot slot)
    {
        var baseMs = GetPollingMs(slot.Descriptor, _options);
        var factor = 1 << Math.Clamp(slot.FailCount - 1, 0, 4);
        return Math.Min(baseMs * factor, _options.MaxBackoffMs);
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
            NextPollAt = DateTimeOffset.MaxValue;
        }

        public DeviceDescriptor Descriptor { get; }
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public ITransport? Transport { get; set; }
        public IDevice? Device { get; set; }
        public DeviceStatusSnapshot Snapshot { get; set; }
        public DateTimeOffset NextPollAt { get; set; }
        public int FailCount { get; set; }
    }
}
