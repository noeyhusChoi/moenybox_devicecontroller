namespace Kiosk.Application.Services.Devices.Deposit;

public interface IDepositService
{
    event EventHandler<DepositEvent>? EventReceived;

    string DeviceId { get; }

    Task<DepositAvailabilityResult> GetAvailabilityAsync(CancellationToken ct = default);
    Task<DepositStartResult> StartDepositAsync(CancellationToken ct = default);
    Task<DepositStopResult> StopDepositAsync(CancellationToken ct = default);
    Task<DepositStackResult> StackAsync(CancellationToken ct = default);
    Task<DepositReturnResult> ReturnAsync(CancellationToken ct = default);
}
