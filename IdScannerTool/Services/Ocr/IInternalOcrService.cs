
namespace IdScannerTool.Services;

public interface IInternalOcrService
{
    Task<RunOcrResultDto> RunAsync(
        string deviceId,
        SaveImageResultDto capture,
        CancellationToken cancellationToken = default);
}
