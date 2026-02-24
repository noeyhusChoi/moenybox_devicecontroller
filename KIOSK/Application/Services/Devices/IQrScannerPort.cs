namespace KIOSK.Application.Services.Devices
{
    public sealed class QrDecodedEventArgs : EventArgs
    {
        public byte BarcodeType { get; init; }
        public string Text { get; init; } = string.Empty;
    }

    public interface IQrScannerPort
    {
        event EventHandler<QrDecodedEventArgs>? Decoded;

        Task EnableAsync(string deviceId, CancellationToken ct = default);
        Task DisableAsync(string deviceId, CancellationToken ct = default);
    }
}
