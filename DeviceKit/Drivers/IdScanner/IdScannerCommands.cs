namespace DeviceKit.Drivers.IdScanner;

public static class IdScannerCommands
{
    public const string ScanStartName = "SCANSTART";
    public const string ScanStopName = "SCANSTOP";
    public const string GetScanStatusName = "GETSCANSTATUS";
    public const string SaveImageName = "SAVEIMAGE";

    public static DeviceCommandRequest ScanStart() => new(ScanStartName);
    public static DeviceCommandRequest ScanStop() => new(ScanStopName);
    public static DeviceCommandRequest GetScanStatus() => new(GetScanStatusName);
    public static DeviceCommandRequest SaveImage() => new(SaveImageName);
}
