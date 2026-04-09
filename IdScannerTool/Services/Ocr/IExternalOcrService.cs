
namespace IdScannerTool.Services;

public interface IExternalOcrService
{
    Task<RunOcrResultDto> RunAsync(
        SaveImageResultDto capture,
        CancellationToken cancellationToken = default);
}
