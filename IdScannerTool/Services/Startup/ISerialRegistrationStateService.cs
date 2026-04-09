namespace IdScannerTool.Services;

public interface ISerialRegistrationStateService
{
    Task<LocalSerialRegistrationState> GetStateAsync(CancellationToken cancellationToken = default);

    Task<StartupRegistrationSaveResult> SaveAsync(string serial, CancellationToken cancellationToken = default);
}
