namespace IdScannerTool.Services;

public interface IOcrHistoryStore
{
    Task<IReadOnlyList<OcrHistoryRow>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(
        IReadOnlyDictionary<string, string> fields,
        string? documentType,
        string rawJson,
        CancellationToken cancellationToken = default);
    Task<int> DeleteByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default);
}
