namespace DeviceKit.Events;

public sealed record QrDecodedPayload(byte BarcodeType, string Text);
