namespace Kiosk.Application.Services.Devices.IdScanner;

public interface IIdScannerService
{
    event EventHandler<IdScannerEvent>? EventReceived;

    string DeviceId { get; }

    Task<DeviceAvailabilityResult> GetAvailabilityAsync(CancellationToken ct = default);
    Task<ScanStartResult> StartScanAsync(CancellationToken ct = default);
    Task<ScanStopResult> StopScanAsync(CancellationToken ct = default);
    Task<ScanCaptureResult> SaveImageAsync(CancellationToken ct = default);
    Task<ScanOcrResult> RunOcrAsync(ScanCaptureResult capture, CancellationToken ct = default);
}
