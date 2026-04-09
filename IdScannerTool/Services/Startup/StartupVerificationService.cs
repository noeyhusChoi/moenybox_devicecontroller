
namespace IdScannerTool.Services;

/// <summary>
/// 앱 시작 게이트(장치 연결/시리얼 추출/비교)를 단일 서비스로 제공한다.
/// </summary>
public sealed class StartupVerificationService : IStartupVerificationService
{
    private const int MinimumStageDurationMs = 2000;
    private const int FailedStageVisibleMs = 450;

    private readonly IDeviceManagerPort _runtimePort;
    private readonly string _deviceId;

    public StartupVerificationService(
        IDeviceManagerPort runtimePort,
        string deviceId)
    {
        _runtimePort = runtimePort;
        _deviceId = deviceId;
    }

    public async Task<StartupVerificationResult> EvaluateStartupAsync(
        string registeredSerial,
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default)
    {
        var localSerial = Normalize(registeredSerial);
        if (string.IsNullOrWhiteSpace(localSerial))
        {
            return new StartupVerificationResult(
                NextStep: StartupNextStep.ShowRegistration,
                StartupStatusMessage: "로컬 시리얼키 미등록",
                StartupDetailMessage: "등록 화면으로 전환합니다.",
                RegisteredSerial: "-",
                ExtractedSerial: "-",
                RegistrationMessage: "로컬 시리얼키 미등록 상태입니다. 등록을 진행하세요.",
                CanRegister: false);
        }

        var extracted = await ExtractForRegistrationAsync(onStageChanged, cancellationToken);
        var deviceSerial = Normalize(extracted.ExtractedSerial);
        if (!extracted.Success || string.IsNullOrWhiteSpace(deviceSerial))
        {
            return new StartupVerificationResult(
                NextStep: StartupNextStep.ShowRegistration,
                StartupStatusMessage: extracted.StartupStatusMessage,
                StartupDetailMessage: extracted.StartupDetailMessage,
                RegisteredSerial: localSerial,
                ExtractedSerial: "-",
                RegistrationMessage: extracted.RegistrationMessage,
                CanRegister: false);
        }

        var matched = await CompareSerialAsync(localSerial, deviceSerial, onStageChanged, cancellationToken);
        if (!matched)
        {
            return new StartupVerificationResult(
                NextStep: StartupNextStep.ShowRegistration,
                StartupStatusMessage: "시리얼 불일치",
                StartupDetailMessage: $"local={localSerial}, device={deviceSerial}",
                RegisteredSerial: localSerial,
                ExtractedSerial: deviceSerial,
                RegistrationMessage: $"시리얼 불일치. local={localSerial}, device={deviceSerial}. 재등록이 필요합니다.",
                CanRegister: true);
        }

        return new StartupVerificationResult(
            NextStep: StartupNextStep.ShowMain,
            StartupStatusMessage: "시리얼 검증 완료",
            StartupDetailMessage: $"local={localSerial}, device={deviceSerial}",
            RegisteredSerial: localSerial,
            ExtractedSerial: deviceSerial,
            RegistrationMessage: "검증 완료",
                CanRegister: false);
    }

    public async Task<StartupRegistrationExtractionResult> ExtractForRegistrationAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default)
    {
        var connected = await RunStageAsync(
            StartupVerificationStage.ConnectDevice,
            "장치 연결 중...",
            "장치 연결 실패",
            ConnectInternalAsync,
            onStageChanged,
            cancellationToken);
        if (!connected)
        {
            return new StartupRegistrationExtractionResult(
                Success: false,
                StartupStatusMessage: "수동 등록: 장치 연결 실패",
                StartupDetailMessage: "장치 연결 상태를 확인하세요.",
                RegistrationMessage: "장치 연결 실패. 장치 상태를 확인하세요.",
                ExtractedSerial: "-",
                CanRegister: false);
        }

        var extractedRaw = await RunStageAsync(
            StartupVerificationStage.ExtractSerial,
            "시리얼 추출 중...",
            "시리얼 추출 실패",
            ExtractInternalAsync,
            onStageChanged,
            cancellationToken);
        var deviceSerial = Normalize(extractedRaw);
        if (string.IsNullOrWhiteSpace(deviceSerial))
        {
            return new StartupRegistrationExtractionResult(
                Success: false,
                StartupStatusMessage: "수동 등록: 시리얼 추출 실패",
                StartupDetailMessage: "장치 시리얼을 읽지 못했습니다.",
                RegistrationMessage: "시리얼 추출 실패. 다시 시도하세요.",
                ExtractedSerial: "-",
                CanRegister: false);
        }

        return new StartupRegistrationExtractionResult(
            Success: true,
            StartupStatusMessage: "수동 등록: 시리얼 추출 완료",
            StartupDetailMessage: $"device={deviceSerial}",
            RegistrationMessage: "시리얼 추출 완료. 등록 버튼으로 로컬 저장을 진행하세요.",
            ExtractedSerial: deviceSerial,
            CanRegister: true);
    }

    public Task<bool> CompareSerialAsync(
        string registeredSerial,
        string extractedSerial,
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default)
    {
        var localSerial = Normalize(registeredSerial);
        var deviceSerial = Normalize(extractedSerial);
        return RunStageAsync(
            StartupVerificationStage.CompareSerial,
            "시리얼 비교 중...",
            "시리얼 비교 불일치",
            () => Task.FromResult(SerialEquals(localSerial, deviceSerial)),
            onStageChanged,
            cancellationToken);
    }

    private async Task<bool> ConnectInternalAsync()
    {
        await _runtimePort.ConnectAsync(_deviceId);
        var connection = await _runtimePort.GetConnectionAsync(_deviceId);
        return connection?.State == DeviceConnectionState.Connected;
    }

    private async Task<string?> ExtractInternalAsync()
    {
        var result = await _runtimePort.ExecuteAsync(_deviceId, new DeviceCommandRequest("GETDEVICEID"));
        if (!result.Success)
        {
            return null;
        }

        var serial = Normalize(result.Data?.ToString());
        return string.IsNullOrWhiteSpace(serial) ? null : serial;
    }

    private static string Normalize(string? serial)
        => (serial ?? string.Empty).Trim().ToUpperInvariant();

    private static bool SerialEquals(string left, string right)
        => string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static async Task<T> RunStageAsync<T>(
        StartupVerificationStage stage,
        string runningMessage,
        string failedMessage,
        Func<Task<T>> action,
        Action<StartupVerificationProgress>? onStageChanged,
        CancellationToken cancellationToken)
    {
        onStageChanged?.Invoke(new StartupVerificationProgress(stage, StartupVerificationStageStatus.Running, runningMessage));

        var start = DateTime.UtcNow;
        var result = await action();

        var elapsed = (int)(DateTime.UtcNow - start).TotalMilliseconds;
        if (elapsed < MinimumStageDurationMs)
        {
            await Task.Delay(MinimumStageDurationMs - elapsed, cancellationToken);
        }

        var success = IsSuccessful(result);
        if (!success)
        {
            onStageChanged?.Invoke(
                new StartupVerificationProgress(
                    stage,
                    StartupVerificationStageStatus.Failed,
                    failedMessage));

            await Task.Delay(FailedStageVisibleMs, cancellationToken);
        }

        return result;
    }

    private static bool IsSuccessful<T>(T result)
        => result switch
        {
            bool boolValue => boolValue,
            string stringValue => !string.IsNullOrWhiteSpace(stringValue),
            _ => result is not null
        };
}
