namespace DeviceKit.Drivers.Withdrawal;

public sealed record WithdrawalEjectRequest(string Value)
{
    public static WithdrawalEjectRequest Default { get; } = new("0");

    internal byte[] ToPayload()
        => System.Text.Encoding.ASCII.GetBytes(string.IsNullOrWhiteSpace(Value) ? "0" : Value);
}
