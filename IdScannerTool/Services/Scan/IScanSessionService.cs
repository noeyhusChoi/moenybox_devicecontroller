
namespace IdScannerTool.Services;

public interface IScanSessionService
{
    event EventHandler<ScanSessionProgress>? ProgressChanged;

    Task<DeviceCommandResponse> StartAsync(CancellationToken cancellationToken = default);
    Task<DeviceCommandResponse> StopAsync(CancellationToken cancellationToken = default);
    Task<ScanSessionProgress> PollOnceAsync(CancellationToken cancellationToken = default);
}
