namespace DeviceKit.Drivers.Withdrawal;

public static class WithdrawalCommands
{
    public const string SensorName = "SENSOR";
    public const string InitName = "INIT";
    public const string EjectName = "EJECT";
    public const string DispenseName = "DISPENSE";

    public static DeviceCommandRequest Sensor() => new(SensorName);
    public static DeviceCommandRequest Init() => new(InitName);
    public static DeviceCommandRequest Eject(WithdrawalEjectRequest request) => new(EjectName, request);
    public static DeviceCommandRequest Dispense(IReadOnlyList<WithdrawalDispenseSlotRequest> requests) => new(DispenseName, requests);
}
