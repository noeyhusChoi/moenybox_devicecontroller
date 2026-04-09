namespace DeviceKit.Drivers.Deposit;

public static class DepositCommands
{
    public const string StartName = "START";
    public const string StopName = "STOP";
    public const string StackName = "STACK";
    public const string ReturnName = "RETURN";

    public static DeviceCommandRequest Start() => new(StartName);
    public static DeviceCommandRequest Stop() => new(StopName);
    public static DeviceCommandRequest Stack() => new(StackName);
    public static DeviceCommandRequest Return() => new(ReturnName);
}
