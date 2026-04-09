namespace IdScannerTool.Services;

public interface IStartupVerificationService
{
    Task<bool> ConnectDeviceAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default);

    Task<string?> ExtractSerialAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default);

    Task<StartupRegistrationExtractionResult> ExtractForRegistrationAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default);
}
