namespace KIOSK.Device.Drivers.IdScanner;

internal enum IdScannerState
{
    Unknown,
    Ready,
    Scanning,
    Completed,
    Error
}

public enum IdScannerScanEvent
{
    Empty,
    Scanning,
    ScanComplete,
    Removed,
    RemovalTimeout
}

internal sealed record IdScanResult(IdScannerState State, string? ImagePath = null, string? Detail = null);

internal static class IdScannerDefaults
{
    public const int NoMoveHoldMs = 1000;
}
