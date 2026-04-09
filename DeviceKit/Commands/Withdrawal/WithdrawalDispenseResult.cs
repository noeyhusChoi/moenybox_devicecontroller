namespace DeviceKit.Commands.Withdrawal;

public sealed record WithdrawalDispenseResult(
    IReadOnlyList<WithdrawalDispenseSlotResult> Slots)
{
    public int TotalSuccessCount => Slots.Sum(static x => x.SuccessCount);
    public int TotalRejectCount => Slots.Sum(static x => x.RejectCount);
}

public sealed record WithdrawalDispenseSlotResult(
    int Slot,
    int SuccessCount,
    int RejectCount);
