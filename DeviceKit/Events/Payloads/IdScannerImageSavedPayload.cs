namespace DeviceKit.Events.Payloads;

public sealed record IdScannerImageSavedPayload(
    int Page,
    string Light,
    string Path);
