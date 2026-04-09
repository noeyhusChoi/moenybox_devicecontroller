namespace IdScannerTool.Services;

public interface IOcrHistoryStore
{
    Task<IReadOnlyList<OcrHistoryRow>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<long> AddAsync(
        IReadOnlyDictionary<string, string> fields,
        string? documentType,
        string? deviceSerial,
        string rawJson,
        CancellationToken cancellationToken = default);
    Task<int> DeleteByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OcrHistoryUsageSyncRow>> GetPendingUsageSyncRowsAsync(
        int maxCount = 100,
        CancellationToken cancellationToken = default);
    Task MarkUsageSyncSucceededAsync(
        long id,
        string? dateKey,
        int? totalUsage,
        string? responseBody,
        CancellationToken cancellationToken = default);
    Task MarkUsageSyncFailedAsync(
        long id,
        string error,
        string? responseBody,
        CancellationToken cancellationToken = default);
}
