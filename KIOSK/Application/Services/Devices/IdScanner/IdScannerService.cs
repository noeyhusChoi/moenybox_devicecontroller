using System.Text.Json;
using DeviceKit.Commands;
using DeviceKit.Commands.IdScanner;
using DeviceKit.Drivers.IdScanner;
using DeviceKit.Engine;
using DeviceKit.Events;
using DeviceKit.Events.Payloads;
using DeviceKit.Status;
using Kiosk.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Logging;

namespace Kiosk.Application.Services.Devices.IdScanner;

public sealed class IdScannerService : IIdScannerService
{
    private const string RunOcrCommandName = "RUNOCR";
    private readonly IDeviceRuntimeService _runtimeService;
    private readonly DeviceRepository _deviceRepository;
    private readonly ILogger<IdScannerService> _logger;
    private readonly SemaphoreSlim _deviceGate = new(1, 1);

    private IDeviceManagerPort? _runtime;
    private string? _deviceId;
    private int _isSubscribed;

    public IdScannerService(
        IDeviceRuntimeService runtimeService,
        DeviceRepository deviceRepository,
        ILogger<IdScannerService> logger)
    {
        _runtimeService = runtimeService;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    public event EventHandler<IdScannerEvent>? EventReceived;

    public string DeviceId => _deviceId ?? string.Empty;

    public async Task<DeviceAvailabilityResult> GetAvailabilityAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
        {
            return new DeviceAvailabilityResult(
                false,
                DeviceAvailabilityState.Unavailable,
                "DEV.IDSCANNER.CONFIG.NOT_FOUND",
                "No configured ID scanner was found.");
        }

        var connection = await session.Runtime.GetConnectionAsync(session.DeviceId, ct).ConfigureAwait(false);
        var status = await session.Runtime.GetStatusAsync(session.DeviceId, ct).ConfigureAwait(false);

        if (connection?.State != DeviceConnectionState.Connected)
        {
            return new DeviceAvailabilityResult(
                false,
                DeviceAvailabilityState.Unavailable,
                "DEV.IDSCANNER.CONNECTION.NOT_CONNECTED",
                "ID scanner is not connected.");
        }

        if (status is null)
        {
            return new DeviceAvailabilityResult(
                false,
                DeviceAvailabilityState.Unknown,
                "DEV.IDSCANNER.STATUS.UNKNOWN",
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

            return new DeviceAvailabilityResult(
                false,
                DeviceAvailabilityState.Unavailable,
                alert.Code,
                alert.Message);
        }

        if (highestSeverity is Severity.Warning)
        {
            var alert = status.Alerts
                .OrderByDescending(x => x.At)
                .First();

            return new DeviceAvailabilityResult(
                true,
                DeviceAvailabilityState.Warning,
                alert.Code,
                alert.Message);
        }

        return new DeviceAvailabilityResult(true, DeviceAvailabilityState.Available);
    }

    public async Task<ScanStartResult> StartScanAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
            return new ScanStartResult(false, "DEV.IDSCANNER.CONFIG.NOT_FOUND", "No configured ID scanner was found.");

        var connected = await session.Runtime.ConnectAsync(session.DeviceId, ct).ConfigureAwait(false);
        if (!connected)
        {
            var connection = await session.Runtime.GetConnectionAsync(session.DeviceId, ct).ConfigureAwait(false);
            if (connection?.State != DeviceConnectionState.Connected)
            {
                return new ScanStartResult(
                    false,
                    "DEV.IDSCANNER.CONNECTION.NOT_CONNECTED",
                    "Failed to connect the ID scanner.");
            }
        }

        var response = await session.Runtime.ExecuteAsync(session.DeviceId, IdScannerCommands.ScanStart(), ct).ConfigureAwait(false);
        return new ScanStartResult(
            response.Success,
            response.Code?.ToString(),
            response.Success ? null : ResolveMessage(response, "Failed to start ID scan."));
    }

    public async Task<ScanStopResult> StopScanAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
            return new ScanStopResult(false, "DEV.IDSCANNER.CONFIG.NOT_FOUND", "No configured ID scanner was found.");

        var response = await session.Runtime.ExecuteAsync(session.DeviceId, IdScannerCommands.ScanStop(), ct).ConfigureAwait(false);
        return new ScanStopResult(
            response.Success,
            response.Code?.ToString(),
            response.Success ? null : ResolveMessage(response, "Failed to stop ID scan."));
    }

    public async Task<ScanCaptureResult> SaveImageAsync(CancellationToken ct = default)
    {
        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
        {
            return new ScanCaptureResult(
                false,
                ErrorCode: "DEV.IDSCANNER.CONFIG.NOT_FOUND",
                ErrorMessage: "No configured ID scanner was found.");
        }

        var response = await session.Runtime.ExecuteAsync(session.DeviceId, IdScannerCommands.SaveImage(), ct).ConfigureAwait(false);
        if (!response.Success)
        {
            return new ScanCaptureResult(
                false,
                ErrorCode: response.Code?.ToString(),
                ErrorMessage: ResolveMessage(response, "Failed to save scanner image."));
        }

        if (response.Data is SaveImageResultDto dto)
        {
            return new ScanCaptureResult(true, dto.ImagePath, dto.ImageByte);
        }

        return new ScanCaptureResult(
            false,
            ErrorCode: "DEV.IDSCANNER.SAVE_IMAGE.INVALID_DATA",
            ErrorMessage: "The scanner returned an unexpected capture payload.");
    }

    public async Task<ScanOcrResult> RunOcrAsync(ScanCaptureResult capture, CancellationToken ct = default)
    {
        if (!capture.Success || capture.ImageBytes is null || capture.ImageBytes.Length == 0)
        {
            return new ScanOcrResult(
                false,
                ErrorCode: "DEV.IDSCANNER.OCR.INVALID_CAPTURE",
                ErrorMessage: "A successful capture payload is required before OCR can run.");
        }

        var session = await TryEnsureSessionAsync(ct).ConfigureAwait(false);
        if (session is null)
        {
            return new ScanOcrResult(
                false,
                ErrorCode: "DEV.IDSCANNER.CONFIG.NOT_FOUND",
                ErrorMessage: "No configured ID scanner was found.");
        }

        var payload = JsonSerializer.Serialize(new SaveImageResultDto(
            capture.ImagePath ?? string.Empty,
            capture.ImageBytes));

        var response = await session.Runtime.ExecuteAsync(
                session.DeviceId,
                new DeviceCommandRequest(RunOcrCommandName, payload),
                ct)
            .ConfigureAwait(false);

        if (!response.Success)
        {
            return new ScanOcrResult(
                false,
                ErrorCode: response.Code?.ToString(),
                ErrorMessage: ResolveMessage(response, "Failed to run scanner OCR."));
        }

        if (response.Data is RunOcrResultDto dto)
        {
            return new ScanOcrResult(
                true,
                dto.DocumentType,
                new Dictionary<string, string>(dto.Fields, StringComparer.OrdinalIgnoreCase));
        }

        return new ScanOcrResult(
            false,
            ErrorCode: "DEV.IDSCANNER.OCR.INVALID_DATA",
            ErrorMessage: "The scanner returned an unexpected OCR payload.");
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
            var scanner = devices.FirstOrDefault(IsIdScannerDevice);
            _deviceId = scanner?.Id;

            if (scanner is not null)
            {
                _logger.LogInformation(
                    "Resolved ID scanner device. deviceId={DeviceId} driverType={DriverType}",
                    scanner.Id,
                    scanner.DriverType);
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

        IdScannerEvent? typedEvent = envelope.EventName switch
        {
            DeviceEventNames.IdScannerDocumentDetected => new IdDocumentDetectedEvent(envelope.DeviceId, envelope.OccurredAt),
            DeviceEventNames.IdScannerScanStatusChanged => MapScanStatusChanged(envelope),
            DeviceEventNames.IdScannerImageSaved => MapImageSaved(envelope),
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
            _logger.LogWarning(ex, "Unhandled exception while dispatching ID scanner event.");
        }
    }

    private static IdScannerEvent? MapScanStatusChanged(DeviceEventEnvelope envelope)
    {
        var payload = DeviceEventJson.Deserialize<IdScannerScanStatusChangedPayload>(envelope.PayloadJson);
        if (payload is null)
            return null;

        return new IdScanStatusChangedEvent(
            envelope.DeviceId,
            envelope.OccurredAt,
            payload.Status switch
            {
                DeviceKit.Events.IdScannerScanStatus.Empty => IdScannerScanPhase.WaitingForDocument,
                DeviceKit.Events.IdScannerScanStatus.Moving => IdScannerScanPhase.Scanning,
                DeviceKit.Events.IdScannerScanStatus.Present => IdScannerScanPhase.Scanning,
                DeviceKit.Events.IdScannerScanStatus.Preparing => IdScannerScanPhase.Scanning,
                DeviceKit.Events.IdScannerScanStatus.NoMove => IdScannerScanPhase.ScanComplete,
                DeviceKit.Events.IdScannerScanStatus.Dirty => IdScannerScanPhase.Faulted,
                _ => IdScannerScanPhase.Faulted
            });
    }

    private static IdScannerEvent? MapImageSaved(DeviceEventEnvelope envelope)
    {
        var payload = DeviceEventJson.Deserialize<IdScannerImageSavedPayload>(envelope.PayloadJson);
        if (payload is null)
            return null;

        return new IdImageSavedEvent(
            envelope.DeviceId,
            envelope.OccurredAt,
            payload.Path);
    }

    private static bool IsIdScannerDevice(Kiosk.Infrastructure.Database.Models.DeviceModel device)
        => string.Equals(device.DriverType, "COMBOSCAN2208", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DeviceType, "IDSCANNER", StringComparison.OrdinalIgnoreCase)
           || string.Equals(device.DeviceType, "ID_SCANNER", StringComparison.OrdinalIgnoreCase);

    private static string ResolveMessage(DeviceCommandResponse response, string fallback)
        => string.IsNullOrWhiteSpace(response.Message) ? fallback : response.Message;

    private sealed record RuntimeSession(IDeviceManagerPort Runtime, string DeviceId);
}
