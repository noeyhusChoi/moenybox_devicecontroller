namespace IdScannerTool.Services;

public interface IStartupVerificationService
{
    Task<StartupVerificationResult> EvaluateStartupAsync(
        string registeredSerial,
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default);

    Task<StartupRegistrationExtractionResult> ExtractForRegistrationAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default);

    Task<bool> CompareSerialAsync(
        string registeredSerial,
        string extractedSerial,
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default);
}
