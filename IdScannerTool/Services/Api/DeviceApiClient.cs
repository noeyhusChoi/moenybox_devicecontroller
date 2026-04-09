using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdScannerTool.Services;

public sealed class DeviceApiClient : IDeviceApiClient
{
    private readonly HttpClient _httpClient;

    public DeviceApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<DeviceApiResponse> GetDeviceAsync(
        string serial,
        string? apiKey,
        CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, serial, apiKey, suffix: string.Empty, expectApiKey: false, cancellationToken);

    public Task<DeviceApiResponse> ActivateDeviceAsync(
        string serial,
        string? apiKey,
        CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, serial, apiKey, suffix: "/activate", expectApiKey: true, cancellationToken);

    public Task<DeviceApiResponse> IncrementUsageAsync(
        string serial,
        string? apiKey,
        CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, serial, apiKey, suffix: "/usage", expectApiKey: false, cancellationToken);

    private async Task<DeviceApiResponse> SendAsync(
        HttpMethod method,
        string serial,
        string? apiKey,
        string suffix,
        bool expectApiKey,
        CancellationToken cancellationToken)
    {
        var normalizedSerial = NormalizeSerial(serial);
        var relativePath = $"client/devices/{Uri.EscapeDataString(normalizedSerial)}{suffix}";
        var requestSummary = BuildRequestSummary(method, relativePath, apiKey);
        if (string.IsNullOrWhiteSpace(normalizedSerial))
        {
            return new DeviceApiResponse(
                Success: false,
                StatusCode: null,
                Status: DeviceApiStatus.InvalidResponse,
                Message: "유효하지 않은 시리얼입니다.",
                RawBody: null,
                RequestSummary: requestSummary,
                ResponseSummary: WriteTrace(requestSummary, "response: invalid serial"));
        }

        try
        {
            using var request = new HttpRequestMessage(
                method,
                relativePath);

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.TryAddWithoutValidation("x-api-key", apiKey.Trim());
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var extractedApiKey = expectApiKey ? TryExtractApiKey(body) : null;
                var successPayload = TryExtractSuccessPayload(body);
                if (expectApiKey && string.IsNullOrWhiteSpace(extractedApiKey))
                {
                    return new DeviceApiResponse(
                        Success: false,
                        StatusCode: (int)response.StatusCode,
                        Status: DeviceApiStatus.InvalidResponse,
                        Message: "활성화 응답에서 API 키를 찾지 못했습니다.",
                        RawBody: body,
                        RequestSummary: requestSummary,
                        ResponseSummary: WriteTrace(requestSummary, BuildResponseSummary(response.StatusCode, body)));
                }

                return new DeviceApiResponse(
                    Success: true,
                    StatusCode: (int)response.StatusCode,
                    Status: DeviceApiStatus.None,
                    Message: successPayload.Message ?? "성공",
                    RawBody: body,
                    ApiKey: extractedApiKey,
                    Serial: successPayload.Serial,
                    DateKey: successPayload.DateKey,
                    TotalUsage: successPayload.TotalUsage,
                    RequestSummary: requestSummary,
                    ResponseSummary: WriteTrace(requestSummary, BuildResponseSummary(response.StatusCode, body)));
            }

            return new DeviceApiResponse(
                Success: false,
                StatusCode: (int)response.StatusCode,
                Status: MapStatus(response.StatusCode),
                Message: BuildFailureMessage(response.StatusCode, body),
                RawBody: body,
                RequestSummary: requestSummary,
                ResponseSummary: WriteTrace(requestSummary, BuildResponseSummary(response.StatusCode, body)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DeviceApiResponse(
                Success: false,
                StatusCode: null,
                Status: DeviceApiStatus.NetworkError,
                Message: $"API 통신 실패: {ex.Message}",
                RawBody: null,
                RequestSummary: requestSummary,
                ResponseSummary: WriteTrace(requestSummary, $"response: exception={ex.Message}"));
        }
    }

    private static string NormalizeSerial(string? serial)
        => (serial ?? string.Empty).Trim();

    private static DeviceApiStatus MapStatus(HttpStatusCode statusCode)
        => statusCode switch
        {
            HttpStatusCode.BadRequest => DeviceApiStatus.BadRequest,
            HttpStatusCode.Unauthorized => DeviceApiStatus.MissingApiKey,
            HttpStatusCode.Forbidden => DeviceApiStatus.InvalidApiKey,
            HttpStatusCode.NotFound => DeviceApiStatus.NotFound,
            _ => DeviceApiStatus.UnexpectedStatus
        };

    private static string BuildFailureMessage(HttpStatusCode statusCode, string? body)
    {
        var errorText = TryExtractErrorMessage(body);
        var defaultMessage = statusCode switch
        {
            HttpStatusCode.BadRequest => "상태 조건이 맞지 않아 요청이 거부되었습니다.-",
            HttpStatusCode.Unauthorized => "API 키가 없습니다.",
            HttpStatusCode.Forbidden => "API 키가 일치하지 않습니다.",
            HttpStatusCode.NotFound => "등록되지 않은 기기입니다.",
            _ => $"예상하지 못한 응답입니다. status={(int)statusCode}"
        };

        return string.IsNullOrWhiteSpace(errorText)
            ? defaultMessage
            : errorText;
    }

    private string BuildRequestSummary(HttpMethod method, string relativePath, string? apiKey)
    {
        var absoluteUrl = new Uri(_httpClient.BaseAddress!, relativePath);
        var headerValue = string.IsNullOrWhiteSpace(apiKey) ? "(none)" : apiKey.Trim();
        return $"request: {method.Method} {absoluteUrl}{Environment.NewLine}x-api-key: {headerValue}";
    }

    private static string BuildResponseSummary(HttpStatusCode statusCode, string? body)
    {
        var bodyText = string.IsNullOrWhiteSpace(body) ? "(empty)" : body;
        return $"response: {(int)statusCode} {statusCode}{Environment.NewLine}body: {bodyText}";
    }

    private static string WriteTrace(string requestSummary, string responseSummary)
    {
        var trace = $"{requestSummary}{Environment.NewLine}{responseSummary}";
        Debug.WriteLine(trace);
        Trace.WriteLine(trace);
        return responseSummary;
    }

    private static string? TryExtractApiKey(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ActivateDeviceResponsePayload>(rawBody);
            return string.IsNullOrWhiteSpace(payload?.Device?.ApiKey)
                ? null
                : payload.Device.ApiKey.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static string? TryExtractErrorMessage(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!document.RootElement.TryGetProperty("error", out var errorElement))
            {
                return null;
            }

            return errorElement.ValueKind == JsonValueKind.String
                ? errorElement.GetString()?.Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static SuccessPayloadInfo TryExtractSuccessPayload(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return SuccessPayloadInfo.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return SuccessPayloadInfo.Empty;
            }

            var message = TryGetString(root, "message");
            var serial = TryGetString(root, "serial");
            var dateKey = TryGetString(root, "dateKey");
            var totalUsage = TryGetInt(root, "totalUsage");

            if (!string.IsNullOrWhiteSpace(serial) || totalUsage.HasValue)
            {
                return new SuccessPayloadInfo(message, serial, dateKey, totalUsage);
            }

            if (root.TryGetProperty("device", out var device) && device.ValueKind == JsonValueKind.Object)
            {
                serial = TryGetString(device, "serial");
                totalUsage = TryGetInt(device, "totalUsage");
                return new SuccessPayloadInfo(message, serial, dateKey, totalUsage);
            }
        }
        catch
        {
        }

        return SuccessPayloadInfo.Empty;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()?.Trim()
            : null;
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        return null;
    }

    private sealed record ActivateDeviceResponsePayload(
        [property: JsonPropertyName("device")] ActivateDevicePayloadDevice? Device);

    private sealed record ActivateDevicePayloadDevice(
        [property: JsonPropertyName("apiKey")] string? ApiKey);

    private sealed record SuccessPayloadInfo(
        string? Message,
        string? Serial,
        string? DateKey,
        int? TotalUsage)
    {
        public static SuccessPayloadInfo Empty { get; } = new(null, null, null, null);
    }
}
