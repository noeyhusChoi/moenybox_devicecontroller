namespace IdScannerTool.Services;

public interface ILocalSerialKeyStore
{
    Task<string?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(string serialKey, CancellationToken cancellationToken = default);
}
