namespace DeviceKit.Drivers.Qr;

public static class QrCommands
{
    public const string EnableName = "SCAN_ENABLE";
    public const string DisableName = "SCAN_DISABLE";

    public static DeviceCommandRequest Enable() => new(EnableName);
    public static DeviceCommandRequest Disable() => new(DisableName);
}
