namespace Kiosk.Application.Services.Devices.Withdrawal;

public enum WithdrawalAvailabilityState
{
    Unknown,
    Available,
    Warning,
    Unavailable
}

public abstract record WithdrawalEvent(
    string DeviceId,
    DateTimeOffset OccurredAt);

public sealed record WithdrawalFaultedEvent(
    string DeviceId,
    DateTimeOffset OccurredAt,
    string Code,
    string Message)
    : WithdrawalEvent(DeviceId, OccurredAt);

public sealed record WithdrawalAvailabilityResult(
    bool IsAvailable,
    WithdrawalAvailabilityState State,
    string? ReasonCode = null,
    string? ReasonMessage = null);

public sealed record WithdrawalStartResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record WithdrawalStopResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record WithdrawalDispenseResult(
    bool Success,
    string DeviceId,
    IReadOnlyList<WithdrawalAllocation> DispensedAllocations,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public decimal DispensedAmount => DispensedAllocations.Sum(x => x.TotalAmount);
}

public sealed record WithdrawalEjectResult(
    bool Success,
    string DeviceId,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record WithdrawalSlotBalance(
    string DeviceId,
    int Slot,
    string CurrencyCode,
    decimal Denomination,
    int Count)
{
    public decimal TotalAmount => Denomination * Count;
}

public sealed record WithdrawalAllocation(
    string DeviceId,
    int Slot,
    string CurrencyCode,
    decimal Denomination,
    int Count)
{
    public decimal TotalAmount => Denomination * Count;
}

public sealed record WithdrawalPlanResult(
    bool Success,
    IReadOnlyList<WithdrawalAllocation> Allocations,
    decimal RemainingAmount = 0m,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record WithdrawalDispenseCommand(
    string DeviceId,
    IReadOnlyList<WithdrawalAllocation> Allocations);
