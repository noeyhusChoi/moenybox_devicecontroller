namespace IdScannerTool.Services;

public interface IStartupSequenceService
{
    Task<StartupSequenceResult> RunStartupAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default);

    Task<StartupRegistrationExtractionResult> ExtractForRegistrationAsync(
        CancellationToken cancellationToken = default);

    Task<StartupRegistrationSaveResult> SaveRegistrationAsync(
        string serial,
        CancellationToken cancellationToken = default);
}

public enum StartupState
{
    Booting = 0,
    CheckingLocalSerial = 1,
    VerifyingSerial = 2,
    NeedsRegistration = 3,
    Ready = 4,
    Failed = 5
}

public sealed record StartupStateTransition(
    StartupState From,
    StartupState To,
    string Reason,
    DateTimeOffset TimestampUtc);

public sealed record StartupSequenceResult(
    StartupState FinalState,
    IReadOnlyList<StartupStateTransition> Transitions,
    StartupNextStep NextStep,
    string StartupStatusMessage,
    string StartupDetailMessage,
    string RegisteredSerial,
    string ExtractedSerial,
    string RegistrationMessage,
    bool CanRegister);
