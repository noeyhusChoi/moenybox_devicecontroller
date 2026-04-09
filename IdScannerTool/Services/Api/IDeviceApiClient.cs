namespace IdScannerTool.Services;

public interface IDeviceApiClient
{
    Task<DeviceApiResponse> GetDeviceAsync(
        string serial,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<DeviceApiResponse> ActivateDeviceAsync(
        string serial,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<DeviceApiResponse> IncrementUsageAsync(
        string serial,
        string? apiKey,
        CancellationToken cancellationToken = default);
}

public enum DeviceApiStatus
{
    None = 0,
    BadRequest = 1,
    MissingApiKey = 2,
    InvalidApiKey = 3,
    NotFound = 4,
    InvalidResponse = 5,
    NetworkError = 6,
    UnexpectedStatus = 7
}

public sealed record DeviceApiResponse(
    bool Success,
    int? StatusCode,
    DeviceApiStatus Status,
    string Message,
    string? RawBody,
    string? ApiKey = null,
    string? Serial = null,
    string? DateKey = null,
    int? TotalUsage = null,
    string? RequestSummary = null,
    string? ResponseSummary = null)
{
    public string Trace
        => string.Join(
            Environment.NewLine,
            new[]
            {
                RequestSummary,
                ResponseSummary
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
