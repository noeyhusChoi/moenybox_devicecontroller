namespace DeviceKit.Events;

public static class DeviceEventNames
{
    public const string StatusChanged = "STATUS_CHANGED";
    public const string Connected = "CONNECTED";
    public const string Disconnected = "DISCONNECTED";
    public const string QrDecoded = "QR_DECODED";
    public const string DepositEscrowed = "DEPOSIT_ESCROWED";
    public const string IdScannerDocumentDetected = "IDSCANNER_DOCUMENT_DETECTED";
    public const string IdScannerScanStatusChanged = "IDSCANNER_SCAN_STATUS_CHANGED";
    public const string IdScannerImageSaved = "IDSCANNER_IMAGE_SAVED";
}
