using DeviceKit.Commands;
using DeviceKit.Drivers.Deposit;
using DeviceKit.Engine;
using DeviceKit.Events;
using DeviceKit.Events.Payloads;
using DeviceKit.Status;
using Kiosk.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Logging;

namespace Kiosk.Application.Services.Devices.Deposit;

public sealed class DepositService : IDepositService
{
    private readonly IDeviceRuntimeService _runtimeService;
    private readonly DeviceRepository _deviceRepository;
    private readonly ILogger<DepositService> _logger;
    private readonly SemaphoreSlim _deviceGate = new(1, 1);

    private IDeviceManagerPort? _runtime;
    private string? _deviceId;
    private int _isSubscribed;

    public DepositService(
        IDeviceRuntimeService runtimeService,
        DeviceRepository deviceRepository,
        ILogger<DepositService> logger)
    {
        _runtimeService = runtimeService;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    public event EventHandler<DepositEvent>? EventReceived;

    public string DeviceId => _deviceId ?? string.Empty;

    public async Task<DepositAvailabilityResult> GetAvailabilityAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
        {
            return new DepositAvailabilityResult(
                false,
                DepositAvailabilityState.Unavailable,
                "DEV.DEPOSIT.CONFIG.NOT_FOUND",
                "No configured deposit device was found.");
        }

        var connection = await session.Runtime.GetConnectionAsync(session.DeviceId, ct).ConfigureAwait(false);
        var status = await session.Runtime.GetStatusAsync(session.DeviceId, ct).ConfigureAwait(false);

        if (connection?.State != DeviceConnectionState.Connected)
        {
            return new DepositAvailabilityResult(
                false,
                DepositAvailabilityState.Unavailable,
                "DEV.DEPOSIT.CONNECTION.NOT_CONNECTED",
                "Deposit device is not connected.");
        }

        if (status is null)
        {
            return new DepositAvailabilityResult(
                false,
                DepositAvailabilityState.Unknown,
                "DEV.DEPOSIT.STATUS.UNKNOWN",
                "No status snapshot is available.");
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

            return new DepositAvailabilityResult(
                false,
                DepositAvailabilityState.Unavailable,
                alert.Code,
                alert.Message);
        }

        if (highestSeverity is Severity.Warning)
        {
            var alert = status.Alerts
                .OrderByDescending(x => x.At)
                .First();

            return new DepositAvailabilityResult(
                true,
                DepositAvailabilityState.Warning,
                alert.Code,
                alert.Message);
        }

        return new DepositAvailabilityResult(true, DepositAvailabilityState.Available);
    }

    public async Task<DepositStartResult> StartDepositAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
            return new DepositStartResult(false, "DEV.DEPOSIT.CONFIG.NOT_FOUND", "No configured deposit device was found.");

        var connected = await session.Runtime.ConnectAsync(session.DeviceId, ct).ConfigureAwait(false);
        if (!connected)
        {
            var connection = await session.Runtime.GetConnectionAsync(session.DeviceId, ct).ConfigureAwait(false);
            if (connection?.State != DeviceConnectionState.Connected)
            {
                return new DepositStartResult(
                    false,
                    "DEV.DEPOSIT.CONNECTION.NOT_CONNECTED",
                    "Failed to connect the deposit device.");
            }
        }

        var response = await session.Runtime.ExecuteAsync(session.DeviceId, DepositCommands.Start(), ct).ConfigureAwait(false);
        return new DepositStartResult(
            response.Success,
            response.Code?.ToString(),
            response.Success ? null : ResolveMessage(response, "Failed to start deposit acceptance."));
    }

    public async Task<DepositStopResult> StopDepositAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
            return new DepositStopResult(false, "DEV.DEPOSIT.CONFIG.NOT_FOUND", "No configured deposit device was found.");

        var response = await session.Runtime.ExecuteAsync(session.DeviceId, DepositCommands.Stop(), ct).ConfigureAwait(false);
        return new DepositStopResult(
            response.Success,
            response.Code?.ToString(),
            response.Success ? null : ResolveMessage(response, "Failed to stop deposit acceptance."));
    }

    public async Task<DepositStackResult> StackAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
            return new DepositStackResult(false, "DEV.DEPOSIT.CONFIG.NOT_FOUND", "No configured deposit device was found.");

        var response = await session.Runtime.ExecuteAsync(session.DeviceId, DepositCommands.Stack(), ct).ConfigureAwait(false);
        return new DepositStackResult(
            response.Success,
            response.Code?.ToString(),
            response.Success ? null : ResolveMessage(response, "Failed to stack deposited cash."));
    }

    public async Task<DepositReturnResult> ReturnAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
            return new DepositReturnResult(false, "DEV.DEPOSIT.CONFIG.NOT_FOUND", "No configured deposit device was found.");

        var response = await session.Runtime.ExecuteAsync(session.DeviceId, DepositCommands.Return(), ct).ConfigureAwait(false);
        return new DepositReturnResult(
            response.Success,
            response.Code?.ToString(),
            response.Success ? null : ResolveMessage(response, "Failed to return deposited cash."));
    }

    private async Task<RuntimeSession?> TryEnsureSessionAsync(CancellationToken ct)
    {
        var runtime = _runtime ?? await _runtimeService.GetPortAsync(ct).ConfigureAwait(false);
        _runtime = runtime;

        EnsureRuntimeSubscription(runtime);

        var deviceId = await ResolveDeviceIdAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(deviceId))
            return null;

        return new RuntimeSession(runtime, deviceId);
    }

    private async Task<string?> ResolveDeviceIdAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_deviceId))
            return _deviceId;

        await _deviceGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrWhiteSpace(_deviceId))
                return _deviceId;

            var devices = await _deviceRepository.LoadAllAsync(ct).ConfigureAwait(false);
            var deposit = devices.FirstOrDefault(IsDepositDevice);
            _deviceId = deposit?.Id;

            if (deposit is not null)
            {
                _logger.LogInformation(
                    "Resolved deposit device. deviceId={DeviceId} driverType={DriverType}",
                    deposit.Id,
                    deposit.DriverType);
            }

            return _deviceId;
        }
        finally
        {
            _deviceGate.Release();
        }
    }

    private void EnsureRuntimeSubscription(IDeviceManagerPort runtime)
    {
        if (Interlocked.Exchange(ref _isSubscribed, 1) == 1)
            return;

        runtime.DeviceEventReceived += OnRuntimeEvent;
    }

    private void OnRuntimeEvent(DeviceEventEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(_deviceId))
            return;

        if (!string.Equals(envelope.DeviceId, _deviceId, StringComparison.OrdinalIgnoreCase))
            return;

        DepositEvent? typedEvent = envelope.EventName switch
        {
            DeviceEventNames.DepositEscrowed => MapEscrowed(envelope),
            _ => null
        };

        if (typedEvent is null)
            return;

        try
        {
            EventReceived?.Invoke(this, typedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unhandled exception while dispatching deposit event.");
        }
    }

    private static DepositEvent? MapEscrowed(DeviceEventEnvelope envelope)
    {
        var payload = DeviceEventJson.Deserialize<DepositEscrowedPayload>(envelope.PayloadJson);
        if (payload is null)
            return null;

        return new DepositEscrowedEvent(
            envelope.DeviceId,
            envelope.OccurredAt,
            payload.Payload);
    }

    private static bool IsDepositDevice(Kiosk.Infrastructure.Database.Models.DeviceModel device)
        => string.Equals(device.DriverType, "SC8307", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DeviceType, "DEPOSIT", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DeviceType, "DEPOSITOR", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DeviceType, "BILL_ACCEPTOR", StringComparison.OrdinalIgnoreCase);

    private static string ResolveMessage(DeviceCommandResponse response, string fallback)
        => string.IsNullOrWhiteSpace(response.Message) ? fallback : response.Message;

    private sealed record RuntimeSession(IDeviceManagerPort Runtime, string DeviceId);
}
