
namespace IdScannerTool.Services;

public enum OcrMode
{
    Auto = 0,
    Internal = 1,
    External = 2
}

public interface IOcrProcessingService
{
    Task<RunOcrResultDto> RunAsync(
        string deviceId,
        SaveImageResultDto capture,
        OcrMode mode = OcrMode.Auto,
        CancellationToken cancellationToken = default);
}
