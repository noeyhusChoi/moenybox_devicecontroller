namespace Kiosk.Application.Services.Devices.IdScanner;

public enum DeviceAvailabilityState
{
    Unknown,
    Available,
    Warning,
    Unavailable
}

public enum IdScannerScanPhase
{
    Idle,
    WaitingForDocument,
    Scanning,
    ScanComplete,
    Removed,
    Timeout,
    Faulted
}

public abstract record IdScannerEvent(
    string DeviceId,
    DateTimeOffset OccurredAt);

public sealed record IdDocumentDetectedEvent(
    string DeviceId,
    DateTimeOffset OccurredAt)
    : IdScannerEvent(DeviceId, OccurredAt);

public sealed record IdScanStatusChangedEvent(
    string DeviceId,
    DateTimeOffset OccurredAt,
    IdScannerScanPhase Phase)
    : IdScannerEvent(DeviceId, OccurredAt);

public sealed record IdImageSavedEvent(
    string DeviceId,
    DateTimeOffset OccurredAt,
    string ImagePath)
    : IdScannerEvent(DeviceId, OccurredAt);

public sealed record IdScannerFaultedEvent(
    string DeviceId,
    DateTimeOffset OccurredAt,
    string Code,
    string Message)
    : IdScannerEvent(DeviceId, OccurredAt);

public sealed record DeviceAvailabilityResult(
    bool IsAvailable,
    DeviceAvailabilityState State,
    string? ReasonCode = null,
    string? ReasonMessage = null);

public sealed record ScanStartResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record ScanStopResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record ScanCaptureResult(
    bool Success,
    string? ImagePath = null,
    byte[]? ImageBytes = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record ScanOcrResult(
    bool Success,
    string? DocumentType = null,
    IReadOnlyDictionary<string, string>? Fields = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);
