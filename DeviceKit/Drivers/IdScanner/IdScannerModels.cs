namespace DeviceKit.Drivers.IdScanner;

internal enum IdScannerState
{
    Unknown,
    Ready,
    Scanning,
    Completed,
    Error
}

internal enum IdScannerScanEvent
{
    Empty,
    Scanning,
    ScanComplete,
    Removed,
    RemovalTimeout
}

internal sealed record IdScanResult(IdScannerState State, string? ImagePath = null, string? Detail = null);
