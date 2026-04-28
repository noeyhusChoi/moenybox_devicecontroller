namespace Kiosk.Application.Services.Devices.Deposit;

public enum DepositAvailabilityState
{
    Unknown,
    Available,
    Warning,
    Unavailable
}

public abstract record DepositEvent(
    string DeviceId,
    DateTimeOffset OccurredAt);

public sealed record DepositEscrowedEvent(
    string DeviceId,
    DateTimeOffset OccurredAt,
    string Payload)
    : DepositEvent(DeviceId, OccurredAt);

public sealed record DepositFaultedEvent(
    string DeviceId,
    DateTimeOffset OccurredAt,
    string Code,
    string Message)
    : DepositEvent(DeviceId, OccurredAt);

public sealed record DepositAvailabilityResult(
    bool IsAvailable,
    DepositAvailabilityState State,
    string? ReasonCode = null,
    string? ReasonMessage = null);

public sealed record DepositStartResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record DepositStopResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record DepositStackResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record DepositReturnResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);
