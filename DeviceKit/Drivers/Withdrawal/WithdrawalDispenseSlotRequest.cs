namespace DeviceKit.Drivers.Withdrawal;

public sealed record WithdrawalDispenseSlotRequest(
    int Slot,
    int Count);
