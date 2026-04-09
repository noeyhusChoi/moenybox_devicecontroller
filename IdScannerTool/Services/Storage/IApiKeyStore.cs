namespace IdScannerTool.Services;

public interface IApiKeyStore
{
    Task<string?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(string apiKey, CancellationToken cancellationToken = default);
}
