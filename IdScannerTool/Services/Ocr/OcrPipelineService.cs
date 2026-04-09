
namespace IdScannerTool.Services;

/// <summary>
/// 내부/외부 OCR 실행 순서를 조율하는 파이프라인.
/// </summary>
public sealed class OcrPipelineService : IOcrProcessingService
{
    private readonly IInternalOcrService _internalOcr;
    private readonly IExternalOcrService _externalOcr;

    public OcrPipelineService(IInternalOcrService internalOcr, IExternalOcrService externalOcr)
    {
        _internalOcr = internalOcr;
        _externalOcr = externalOcr;
    }

    public async Task<RunOcrResultDto> RunAsync(
        string deviceId,
        SaveImageResultDto capture,
        OcrMode mode = OcrMode.Auto,
        CancellationToken cancellationToken = default)
    {
        return mode switch
        {
            OcrMode.Internal => await _internalOcr.RunAsync(deviceId, capture, cancellationToken).ConfigureAwait(false),
            OcrMode.External => await _externalOcr.RunAsync(capture, cancellationToken).ConfigureAwait(false),
            _ => await RunAutoAsync(deviceId, capture, cancellationToken).ConfigureAwait(false)
        };
    }

    private async Task<RunOcrResultDto> RunAutoAsync(
        string deviceId,
        SaveImageResultDto capture,
        CancellationToken cancellationToken)
    {
        var internalResult = await _internalOcr.RunAsync(deviceId, capture, cancellationToken).ConfigureAwait(false);
        if (internalResult.Success)
        {
            return internalResult;
        }

        return await _externalOcr.RunAsync(capture, cancellationToken).ConfigureAwait(false);
    }
}
