namespace DeviceController.Devices.Scanner
{
    public record ScannerRevision(string Model, string Firmware, string Hardware);

    public record ScannerDecodeData(byte BarcodeType, string Payload);

    public record ScannerDeviceConfig(string DeviceId, string PortName, int BaudRate);
}
