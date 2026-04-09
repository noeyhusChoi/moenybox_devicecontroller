namespace IdScannerTool.Services;

public enum StartupNextStep
{
    ShowMain = 0,
    ShowRegistration = 1
}

public enum StartupVerificationStage
{
    ConnectDevice = 0,
    ExtractSerial = 1,
    CompareSerial = 2
}

public enum StartupVerificationStageStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

public sealed record StartupVerificationProgress(
    StartupVerificationStage Stage,
    StartupVerificationStageStatus Status,
    string Message);

public sealed record StartupVerificationResult(
    StartupNextStep NextStep,
    string StartupStatusMessage,
    string StartupDetailMessage,
    string RegisteredSerial,
    string ExtractedSerial,
    string RegistrationMessage,
    bool CanRegister);

public sealed record StartupRegistrationExtractionResult(
    bool Success,
    string StartupStatusMessage,
    string StartupDetailMessage,
    string RegistrationMessage,
    string ExtractedSerial,
    bool CanRegister);

public sealed record StartupRegistrationSaveResult(
    bool Success,
    string StartupStatusMessage,
    string StartupDetailMessage,
    string RegistrationMessage);

public sealed record LocalSerialRegistrationState(
    bool IsRegistered,
    string RegisteredSerial);
