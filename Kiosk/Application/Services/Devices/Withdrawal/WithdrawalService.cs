using Kiosk.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Logging;
using DeviceKitWithdrawalDispenseResult = DeviceKit.Commands.Withdrawal.WithdrawalDispenseResult;
using DeviceKitWithdrawalDispenseSlotResult = DeviceKit.Commands.Withdrawal.WithdrawalDispenseSlotResult;

namespace Kiosk.Application.Services.Devices.Withdrawal;

public sealed class WithdrawalService : IWithdrawalService
{
    private readonly IDeviceRuntimeService _runtimeService;
    private readonly DeviceRepository _deviceRepository;
    private readonly ILogger<WithdrawalService> _logger;
    private readonly SemaphoreSlim _deviceGate = new(1, 1);

    private IDeviceManagerPort? _runtime;
    private IReadOnlyList<string>? _deviceIds;

    public WithdrawalService(
        IDeviceRuntimeService runtimeService,
        DeviceRepository deviceRepository,
        ILogger<WithdrawalService> logger)
    {
        _runtimeService = runtimeService;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    public event EventHandler<WithdrawalEvent>? EventReceived;

    public async Task<WithdrawalAvailabilityResult> GetAvailabilityAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null || session.DeviceIds.Count == 0)
        {
            return new WithdrawalAvailabilityResult(
                false,
                WithdrawalAvailabilityState.Unavailable,
                "DEV.WITHDRAWAL.CONFIG.NOT_FOUND",
                "No configured withdrawal device was found.");
        }

        foreach (var deviceId in session.DeviceIds)
        {
            var connection = await session.Runtime.GetConnectionAsync(deviceId, ct).ConfigureAwait(false);
            var status = await session.Runtime.GetStatusAsync(deviceId, ct).ConfigureAwait(false);

            if (connection?.State != DeviceConnectionState.Connected)
            {
                return new WithdrawalAvailabilityResult(
                    false,
                    WithdrawalAvailabilityState.Unavailable,
                    "DEV.WITHDRAWAL.CONNECTION.NOT_CONNECTED",
                    $"Withdrawal device '{deviceId}' is not connected.");
            }

            if (status is null)
            {
                return new WithdrawalAvailabilityResult(
                    false,
                    WithdrawalAvailabilityState.Unknown,
                    "DEV.WITHDRAWAL.STATUS.UNKNOWN",
                    $"No status snapshot is available for '{deviceId}'.");
            }

            var highestSeverity = status.Alerts.Count == 0
                ? (Severity?)null
                : status.Alerts.Max(alert => alert.Severity);

            if (highestSeverity is Severity.Error or Severity.Critical)
            {
                var alert = status.Alerts
                    .OrderByDescending(x => x.Severity)
                    .ThenByDescending(x => x.At)
                    .First();

                return new WithdrawalAvailabilityResult(
                    false,
                    WithdrawalAvailabilityState.Unavailable,
                    alert.Code,
                    alert.Message);
            }

            if (highestSeverity is Severity.Warning)
            {
                var alert = status.Alerts
                    .OrderByDescending(x => x.At)
                    .First();

                return new WithdrawalAvailabilityResult(
                    true,
                    WithdrawalAvailabilityState.Warning,
                    alert.Code,
                    alert.Message);
            }
        }

        return new WithdrawalAvailabilityResult(true, WithdrawalAvailabilityState.Available);
    }

    public async Task<WithdrawalStartResult> StartAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null || session.DeviceIds.Count == 0)
            return new WithdrawalStartResult(false, "DEV.WITHDRAWAL.CONFIG.NOT_FOUND", "No configured withdrawal device was found.");

        foreach (var deviceId in session.DeviceIds)
        {
            var connected = await session.Runtime.ConnectAsync(deviceId, ct).ConfigureAwait(false);
            if (!connected)
            {
                var connection = await session.Runtime.GetConnectionAsync(deviceId, ct).ConfigureAwait(false);
                if (connection?.State != DeviceConnectionState.Connected)
                {
                    return new WithdrawalStartResult(
                        false,
                        "DEV.WITHDRAWAL.CONNECTION.NOT_CONNECTED",
                    $"Failed to connect withdrawal device '{deviceId}'.");
                }
            }
            var init = await session.Runtime.ExecuteAsync(deviceId, WithdrawalCommands.Init(), ct).ConfigureAwait(false);
            if (!init.Success)
            {
                return new WithdrawalStartResult(
                    false,
                    init.Code?.ToString(),
                    ResolveMessage(init, $"Failed to initialize withdrawal device '{deviceId}'."));
            }
        }

        return new WithdrawalStartResult(true);
    }

    public async Task<WithdrawalStopResult> StopAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null || session.DeviceIds.Count == 0)
            return new WithdrawalStopResult(false, "DEV.WITHDRAWAL.CONFIG.NOT_FOUND", "No configured withdrawal device was found.");

        return new WithdrawalStopResult(true);
    }

    public async Task<WithdrawalDispenseResult> DispenseAsync(WithdrawalDispenseCommand command, CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null || session.DeviceIds.Count == 0)
            return new WithdrawalDispenseResult(false, command.DeviceId, [], "DEV.WITHDRAWAL.CONFIG.NOT_FOUND", "No configured withdrawal device was found.");

        var requests = command.Allocations
            .GroupBy(x => x.Slot)
            .Select(g => new WithdrawalDispenseSlotRequest(g.Key, checked(g.Sum(x => x.Count))))
            .ToArray();

        var response = await session.Runtime.ExecuteAsync(command.DeviceId, WithdrawalCommands.Dispense(requests), ct).ConfigureAwait(false);
        var dispensedAllocations = ResolveDispensedAllocations(command, response);
        return new WithdrawalDispenseResult(
            response.Success,
            command.DeviceId,
            dispensedAllocations,
            response.Code?.ToString(),
            response.Success ? null : ResolveMessage(response, $"Failed to dispense from '{command.DeviceId}'."));
    }

    public async Task<WithdrawalEjectResult> EjectAsync(string deviceId, string value = "0", CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null || session.DeviceIds.Count == 0)
            return new WithdrawalEjectResult(false, deviceId, "DEV.WITHDRAWAL.CONFIG.NOT_FOUND", "No configured withdrawal device was found.");

        var response = await session.Runtime.ExecuteAsync(deviceId, WithdrawalCommands.Eject(new WithdrawalEjectRequest(value)), ct).ConfigureAwait(false);
        return new WithdrawalEjectResult(
            response.Success,
            deviceId,
            response.Code?.ToString(),
            response.Success ? null : ResolveMessage(response, $"Failed to eject from '{deviceId}'."));
    }

    private async Task<RuntimeSession?> TryEnsureSessionAsync(CancellationToken ct)
    {
        var runtime = _runtime ?? await _runtimeService.GetPortAsync(ct).ConfigureAwait(false);
        _runtime = runtime;

        var deviceIds = await ResolveDeviceIdsAsync(ct).ConfigureAwait(false);
        if (deviceIds.Count == 0)
            return null;

        return new RuntimeSession(runtime, deviceIds);
    }

    private async Task<IReadOnlyList<string>> ResolveDeviceIdsAsync(CancellationToken ct)
    {
        if (_deviceIds is { Count: > 0 })
            return _deviceIds;

        await _deviceGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_deviceIds is { Count: > 0 })
                return _deviceIds;

            var devices = await _deviceRepository.LoadAllAsync(ct).ConfigureAwait(false);
            _deviceIds = devices
                .Where(IsWithdrawalDevice)
                .Select(x => x.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _logger.LogInformation("Resolved withdrawal devices. count={Count}", _deviceIds.Count);

            return _deviceIds;
        }
        finally
        {
            _deviceGate.Release();
        }
    }

    private static bool IsWithdrawalDevice(Kiosk.Infrastructure.Database.Models.DeviceModel device)
        => string.Equals(device.DriverType, "HCDM10K", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DriverType, "HCDM20K", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DriverType, "LCDM4000", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DriverType, "LCDM-4000", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DeviceType, "WITHDRAWAL", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DeviceType, "DISPENSER", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DeviceType, "CASH_DISPENSER", StringComparison.OrdinalIgnoreCase);

    private static string ResolveMessage(DeviceCommandResponse response, string fallback)
        => string.IsNullOrWhiteSpace(response.Message) ? fallback : response.Message;

    private static IReadOnlyList<WithdrawalAllocation> ResolveDispensedAllocations(
        WithdrawalDispenseCommand command,
        DeviceCommandResponse response)
    {
        if (response.Data is DeviceKitWithdrawalDispenseResult result && result.Slots.Count > 0)
            return MapDispensedAllocations(command.Allocations, result.Slots);

        return response.Success ? command.Allocations.ToArray() : [];
    }

    private static IReadOnlyList<WithdrawalAllocation> MapDispensedAllocations(
        IReadOnlyList<WithdrawalAllocation> plannedAllocations,
        IReadOnlyList<DeviceKitWithdrawalDispenseSlotResult> slotResults)
    {
        var remainingBySlot = slotResults
            .GroupBy(x => x.Slot)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SuccessCount));

        var actualAllocations = new List<WithdrawalAllocation>(plannedAllocations.Count);
        foreach (var allocation in plannedAllocations)
        {
            if (!remainingBySlot.TryGetValue(allocation.Slot, out var remaining) || remaining <= 0)
                continue;

            var dispensedCount = Math.Min(allocation.Count, remaining);
            if (dispensedCount <= 0)
                continue;

            actualAllocations.Add(allocation with { Count = dispensedCount });
            remainingBySlot[allocation.Slot] = remaining - dispensedCount;
        }

        return actualAllocations;
    }

    private sealed record RuntimeSession(IDeviceManagerPort Runtime, IReadOnlyList<string> DeviceIds);
}
