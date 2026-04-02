using System.IO;
using System.Text.Json;

namespace IdScannerTool.Services;

public sealed record ExternalOcrOptions(
    string InputDir,
    string ResultDir,
    string ResultTypeDir,
    TimeSpan ResultTimeout,
    TimeSpan PollInterval);

public sealed class ExternalOcrService : IExternalOcrService
{
    private readonly ExternalOcrOptions _options;

    public ExternalOcrService(ExternalOcrOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.InputDir);
        Directory.CreateDirectory(_options.ResultDir);
        Directory.CreateDirectory(_options.ResultTypeDir);
    }

    public async Task<RunOcrResultDto> RunAsync(
        SaveImageResultDto capture,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(capture.ImagePath) || !File.Exists(capture.ImagePath))
        {
            return Fail("Invalid image path for external OCR.");
        }

        var sessionId = EnsureFourDigits(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        var job = BuildJob(sessionId);

        try
        {
            await DeleteResultsAsync(job, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            File.Copy(capture.ImagePath, job.InfraImagePath, overwrite: true);
            if (!string.IsNullOrWhiteSpace(capture.WhiteImagePath) && File.Exists(capture.WhiteImagePath))
            {
                File.Copy(capture.WhiteImagePath, job.WhiteImagePath, overwrite: true);
            }

            using (File.Create(job.TriggerPath))
            {
            }

            var (typeJson, resultJson) = await WaitForResultsAsync(
                job,
                _options.ResultTimeout,
                _options.PollInterval,
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(typeJson) || string.IsNullOrWhiteSpace(resultJson))
            {
                return Fail("Timed out waiting for external OCR results.");
            }

            return ParseExternalResult(typeJson, resultJson);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    private RunOcrResultDto ParseExternalResult(string typeJson, string resultJson)
    {
        string? documentType = null;
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using (var typeDoc = JsonDocument.Parse(typeJson))
        {
            documentType = GetString(typeDoc.RootElement, "type");
        }

        using (var resultDoc = JsonDocument.Parse(resultJson))
        {
            AddIfNotEmpty(fields, "type", GetString(resultDoc.RootElement, "type"));
            AddIfNotEmpty(fields, "id", GetString(resultDoc.RootElement, "id"));
            AddIfNotEmpty(fields, "name", GetString(resultDoc.RootElement, "name"));
            AddIfNotEmpty(fields, "address", GetString(resultDoc.RootElement, "address"));
            AddIfNotEmpty(fields, "nation", GetString(resultDoc.RootElement, "nation"));
            AddIfNotEmpty(fields, "comment", GetString(resultDoc.RootElement, "comment"));
            AddIfNotEmpty(fields, "rotate", GetString(resultDoc.RootElement, "rotate_image"));

            AddIfNotEmpty(fields, "NO", GetString(resultDoc.RootElement, "id"));
            AddIfNotEmpty(fields, "NAME", GetString(resultDoc.RootElement, "name"));
            AddIfNotEmpty(fields, "NATIONALITY", GetString(resultDoc.RootElement, "nation"));
            AddIfNotEmpty(fields, "DOCUMENTTYPE", GetString(resultDoc.RootElement, "type"));
        }

        if (fields.Count == 0)
        {
            return Fail("External OCR returned empty fields.");
        }

        return new RunOcrResultDto(
            Success: true,
            Source: "External",
            DocumentType: documentType,
            Fields: fields,
            Error: null);
    }

    private static void AddIfNotEmpty(IDictionary<string, string> fields, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields[key] = value;
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private ExternalOcrJob BuildJob(string sessionId)
        => new(
            SessionId: sessionId,
            WhiteImagePath: Path.Combine(_options.InputDir, $"{sessionId}_White.jpg"),
            InfraImagePath: Path.Combine(_options.InputDir, $"{sessionId}_Infra.jpg"),
            TriggerPath: Path.Combine(_options.InputDir, $"{sessionId}_Infra.ocr"),
            TypeJsonPath: Path.Combine(_options.ResultTypeDir, $"{sessionId}_Infra.json"),
            ResultJsonPath: Path.Combine(_options.ResultDir, $"{sessionId}_Infra.json"));

    private static string EnsureFourDigits(string value)
    {
        if (int.TryParse(value, out var number))
        {
            return number.ToString("0000");
        }

        return Math.Abs(value.GetHashCode() % 10000).ToString("0000");
    }

    private static async Task<(string? typeJson, string? resultJson)> WaitForResultsAsync(
        ExternalOcrJob job,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (sw.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            if (File.Exists(job.TypeJsonPath) && File.Exists(job.ResultJsonPath))
            {
                var remaining = timeout - sw.Elapsed;
                var typeJson = await ReadAllTextWhenReadyAsync(job.TypeJsonPath, remaining, cancellationToken).ConfigureAwait(false);
                var resultJson = await ReadAllTextWhenReadyAsync(job.ResultJsonPath, remaining, cancellationToken).ConfigureAwait(false);
                return (typeJson, resultJson);
            }

            await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
        }

        return (null, null);
    }

    private static async Task<string?> ReadAllTextWhenReadyAsync(
        string path,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        TimeSpan? stableWindow = null,
        TimeSpan? checkInterval = null)
    {
        stableWindow ??= TimeSpan.FromMilliseconds(250);
        checkInterval ??= TimeSpan.FromMilliseconds(100);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        long? lastLength = null;
        DateTime? lastWrite = null;
        var stableSince = DateTime.UtcNow;

        while (sw.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var fi = new FileInfo(path);
                if (!fi.Exists)
                {
                    await Task.Delay(checkInterval.Value, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (lastLength == fi.Length && lastWrite == fi.LastWriteTimeUtc)
                {
                    if (DateTime.UtcNow - stableSince >= stableWindow.Value)
                    {
                        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        using var sr = new StreamReader(fs);
                        return await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    lastLength = fi.Length;
                    lastWrite = fi.LastWriteTimeUtc;
                    stableSince = DateTime.UtcNow;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            await Task.Delay(checkInterval.Value, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task DeleteResultsAsync(
        ExternalOcrJob job,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var half = TimeSpan.FromMilliseconds(Math.Max(100, timeout.TotalMilliseconds / 2));
        _ = await DeleteFileWhenReadyAsync(job.TypeJsonPath, half, cancellationToken).ConfigureAwait(false);
        _ = await DeleteFileWhenReadyAsync(job.ResultJsonPath, half, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> DeleteFileWhenReadyAsync(
        string path,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return true;
                }

                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
                return true;
            }
            catch (IOException)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    private static RunOcrResultDto Fail(string error)
        => new(
            Success: false,
            Source: "External",
            DocumentType: null,
            Fields: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            Error: error);

    private sealed record ExternalOcrJob(
        string SessionId,
        string WhiteImagePath,
        string InfraImagePath,
        string TriggerPath,
        string TypeJsonPath,
        string ResultJsonPath);
}
