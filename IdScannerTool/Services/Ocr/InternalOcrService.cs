using System.Text.Json;

namespace IdScannerTool.Services;

public sealed class InternalOcrService : IInternalOcrService
{
    private readonly IDeviceManagerPort _runtimePort;

    public InternalOcrService(IDeviceManagerPort runtimePort)
    {
        _runtimePort = runtimePort;
    }

    public async Task<RunOcrResultDto> RunAsync(
        string deviceId,
        SaveImageResultDto capture,
        CancellationToken cancellationToken = default)
    {
        if (capture.ImageByte is not { Length: > 0 })
        {
            return Failed("Internal", "Document bytes are empty.");
        }

        try
        {
            var payload = JsonSerializer.Serialize(capture);
            var result = await _runtimePort.ExecuteAsync(
                deviceId,
                new DeviceCommandRequest("RUNOCR", payload),
                cancellationToken).ConfigureAwait(false);

            if (!result.Success)
            {
                return Failed("Internal", result.Message, result.Code?.ToString());
            }

            if (result.Data is RunOcrResultDto ocrDto)
            {
                return ocrDto with { Source = "Internal" };
            }

            if (result.Data is Dictionary<string, string> fieldsByObject && fieldsByObject.Count > 0)
            {
                fieldsByObject.TryGetValue("DOCUMENTTYPE", out var documentType);
                return new RunOcrResultDto(
                    Success: true,
                    Source: "Internal",
                    DocumentType: string.IsNullOrWhiteSpace(documentType) ? null : documentType,
                    Fields: fieldsByObject,
                    Error: null);
            }

            if (result.Data is string raw && !string.IsNullOrWhiteSpace(raw))
            {
                var dto = JsonSerializer.Deserialize<RunOcrResultDto>(raw);
                if (dto is not null)
                    return dto with { Source = "Internal" };

                var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
                if (fields is { Count: > 0 })
                {
                    fields.TryGetValue("DOCUMENTTYPE", out var documentType);
                    return new RunOcrResultDto(
                        Success: true,
                        Source: "Internal",
                        DocumentType: string.IsNullOrWhiteSpace(documentType) ? null : documentType,
                        Fields: fields,
                        Error: null);
                }
            }

            return Failed("Internal", "OCR result parsing failed.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Failed("Internal", ex.Message);
        }
    }

    private static RunOcrResultDto Failed(string source, string? message, string? code = null)
    {
        var error = string.IsNullOrWhiteSpace(code)
            ? (message ?? "Unknown OCR error.")
            : $"{code}: {message}";

        return new RunOcrResultDto(
            Success: false,
            Source: source,
            DocumentType: null,
            Fields: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            Error: error);
    }
}
