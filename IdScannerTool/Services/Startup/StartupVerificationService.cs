namespace IdScannerTool.Services;

/// <summary>
/// 앱 시작 게이트(장치 연결/시리얼 추출)를 단일 서비스로 제공한다.
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

    public Task<bool> ConnectDeviceAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default)
    {
        return RunStageAsync(
            StartupVerificationStage.ConnectDevice,
            "장치 연결 중...",
            "장치 연결 실패",
            ConnectInternalAsync,
            onStageChanged,
            cancellationToken);
    }

    public Task<string?> ExtractSerialAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default)
    {
        return RunStageAsync(
            StartupVerificationStage.ExtractSerial,
            "시리얼 추출 중...",
            "시리얼 추출 실패",
            ExtractInternalAsync,
            onStageChanged,
            cancellationToken);
    }

    public async Task<StartupRegistrationExtractionResult> ExtractForRegistrationAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default)
    {
        var connected = await ConnectDeviceAsync(onStageChanged, cancellationToken);
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

        var extractedRaw = await ExtractSerialAsync(onStageChanged, cancellationToken);
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
            RegistrationMessage: "시리얼 추출 완료. 활성화 버튼으로 API 키 발급을 진행하세요.",
            ExtractedSerial: deviceSerial,
            CanRegister: true);
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

        return ToRegistrationSerial(result.Data?.ToString());
    }

    private static string Normalize(string? serial)
        => (serial ?? string.Empty).Trim().ToUpperInvariant();

    private static string? ToRegistrationSerial(string? rawSerial)
    {
        var normalized = Normalize(rawSerial);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());
        var baseSerial = digitsOnly.Length >= 7
            ? digitsOnly[^7..]
            : normalized.Length >= 7
                ? normalized[^7..]
                : normalized;

        if (string.IsNullOrWhiteSpace(baseSerial))
        {
            return null;
        }

        var chars = baseSerial.ToCharArray();
        chars[0] = '1';
        return new string(chars);
    }

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
