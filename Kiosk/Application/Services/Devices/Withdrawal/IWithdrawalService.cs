namespace Kiosk.Application.Services.Devices.Withdrawal;

public interface IWithdrawalService
{
    event EventHandler<WithdrawalEvent>? EventReceived;

    Task<WithdrawalAvailabilityResult> GetAvailabilityAsync(CancellationToken ct = default);
    Task<WithdrawalStartResult> StartAsync(CancellationToken ct = default);
    Task<WithdrawalStopResult> StopAsync(CancellationToken ct = default);
    Task<WithdrawalDispenseResult> DispenseAsync(WithdrawalDispenseCommand command, CancellationToken ct = default);
    Task<WithdrawalEjectResult> EjectAsync(string deviceId, string value = "0", CancellationToken ct = default);
}
