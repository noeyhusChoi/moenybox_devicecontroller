namespace IdScannerTool.Services;

public sealed class StartupSequenceService : IStartupSequenceService
{
    private readonly IStartupVerificationService _startupVerificationService;
    private readonly IApiKeyStore _apiKeyStore;
    private readonly IDeviceApiClient _deviceApiClient;

    public StartupSequenceService(
        IStartupVerificationService startupVerificationService,
        IApiKeyStore apiKeyStore,
        IDeviceApiClient deviceApiClient)
    {
        _startupVerificationService = startupVerificationService;
        _apiKeyStore = apiKeyStore;
        _deviceApiClient = deviceApiClient;
    }

    public async Task<StartupSequenceResult> RunStartupAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default)
    {
        var stateMachine = new StartupStateMachine();
        stateMachine.MoveToVerifyingSerial("장치 연결 시작");

        try
        {
            var connected = await _startupVerificationService.ConnectDeviceAsync(onStageChanged, cancellationToken);
            if (!connected)
            {
                stateMachine.MoveToFailed("장치 연결 실패");
                return BuildFailure(
                    stateMachine,
                    startupStatusMessage: "장치 연결 실패",
                    startupDetailMessage: "장치 연결 상태를 확인하세요.",
                    registrationMessage: "장치 연결에 실패했습니다. 장치를 확인하세요.",
                    canRegister: false);
            }

            stateMachine.MoveToCheckingLocalSerial("API 키 확인");
            var savedApiKey = await CheckApiKeyAsync(onStageChanged, cancellationToken);

            stateMachine.MoveToVerifyingSerial("시리얼 추출");
            var extractedSerial = Normalize(await _startupVerificationService.ExtractSerialAsync(onStageChanged, cancellationToken));
            if (string.IsNullOrWhiteSpace(extractedSerial))
            {
                stateMachine.MoveToFailed("시리얼 추출 실패");
                return BuildFailure(
                    stateMachine,
                    startupStatusMessage: "시리얼 추출 실패",
                    startupDetailMessage: "장치 시리얼을 읽지 못했습니다.",
                    registrationMessage: "시리얼 추출에 실패했습니다. 다시 시도하세요.",
                    canRegister: false,
                    extractedSerial: "-",
                    registeredSerial: savedApiKey ?? "-");
            }

            if (!string.IsNullOrWhiteSpace(savedApiKey))
            {
                var lookup = await VerifyDeviceAsync(
                    operationName: "등록 기기 조회",
                    apiCall: ct => _deviceApiClient.GetDeviceAsync(extractedSerial, savedApiKey, ct),
                    onStageChanged,
                    cancellationToken);

                if (!lookup.Success)
                {
                    stateMachine.MoveToFailed("등록 기기 조회 실패");
                    return BuildFailure(
                        stateMachine,
                        startupStatusMessage: "등록 기기 조회 실패",
                        startupDetailMessage: BuildApiDetail(lookup),
                        registrationMessage: $"저장된 API 키로 기기 검증에 실패했습니다.{Environment.NewLine}{BuildApiDetail(lookup)}",
                        canRegister: true,
                        extractedSerial: extractedSerial,
                        registeredSerial: savedApiKey);
                }

                stateMachine.MoveToReady("API 키 검증 성공");
                return new StartupSequenceResult(
                    FinalState: stateMachine.CurrentState,
                    Transitions: stateMachine.Transitions,
                    NextStep: StartupNextStep.ShowMain,
                    StartupStatusMessage: "기기 검증 완료",
                    StartupDetailMessage: BuildApiDetail(lookup),
                    RegisteredSerial: savedApiKey,
                    ExtractedSerial: extractedSerial,
                    RegistrationMessage: $"저장된 API 키로 기기 검증을 완료했습니다.{Environment.NewLine}{BuildApiDetail(lookup)}",
                    CanRegister: false);
            }

            var activate = await VerifyDeviceAsync(
                operationName: "기기 활성화",
                apiCall: ct => _deviceApiClient.ActivateDeviceAsync(extractedSerial, apiKey: null, ct),
                onStageChanged,
                cancellationToken);

            if (!activate.Success)
            {
                stateMachine.MoveToFailed("기기 활성화 실패");
                return BuildFailure(
                    stateMachine, 
                    startupStatusMessage: "기기 활성화 실패",
                    startupDetailMessage: BuildApiDetail(activate),
                    registrationMessage: $"기기 활성화에 실패했습니다.{Environment.NewLine}{BuildApiDetail(activate)}",
                    canRegister: true,
                    extractedSerial: extractedSerial);
            }

            var issuedApiKey = Normalize(activate.ApiKey);
            if (string.IsNullOrWhiteSpace(issuedApiKey))
            {
                stateMachine.MoveToFailed("활성화 응답 API 키 누락");
                return BuildFailure(
                    stateMachine,
                    startupStatusMessage: "API 키 저장 실패",
                    startupDetailMessage: "활성화 응답에 API 키가 없습니다.",
                    registrationMessage: "활성화 응답에서 API 키를 받지 못했습니다.",
                    canRegister: true,
                    extractedSerial: extractedSerial);
            }

            await _apiKeyStore.SaveAsync(issuedApiKey, cancellationToken);

            stateMachine.MoveToReady("기기 활성화 및 API 키 저장 완료");
            return new StartupSequenceResult(
                FinalState: stateMachine.CurrentState,
                Transitions: stateMachine.Transitions,
                NextStep: StartupNextStep.ShowMain,
                StartupStatusMessage: "기기 활성화 완료",
                StartupDetailMessage: BuildApiDetail(activate),
                RegisteredSerial: issuedApiKey,
                ExtractedSerial: extractedSerial,
                RegistrationMessage: $"기기 활성화 후 API 키 저장을 완료했습니다.{Environment.NewLine}{BuildApiDetail(activate)}",
                CanRegister: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stateMachine.MoveToFailed("시작 시퀀스 오류");
            return BuildFailure(
                stateMachine,
                startupStatusMessage: "시작 시퀀스 오류",
                startupDetailMessage: ex.Message,
                registrationMessage: $"오류: {ex.Message}",
                canRegister: true);
        }
    }

    public Task<StartupRegistrationExtractionResult> ExtractForRegistrationAsync(
        CancellationToken cancellationToken = default)
        => _startupVerificationService.ExtractForRegistrationAsync(cancellationToken: cancellationToken);

    public async Task<StartupRegistrationSaveResult> SaveRegistrationAsync(
        string serial,
        CancellationToken cancellationToken = default)
    {
        var extractedSerial = Normalize(serial);
        if (string.IsNullOrWhiteSpace(extractedSerial))
        {
            return new StartupRegistrationSaveResult(
                Success: false,
                StartupStatusMessage: "기기 활성화 실패",
                StartupDetailMessage: "유효하지 않은 시리얼",
                RegistrationMessage: "활성화할 시리얼이 없습니다.");
        }

        var response = await _deviceApiClient.ActivateDeviceAsync(extractedSerial, apiKey: null, cancellationToken);
        if (!response.Success)
        {
            return new StartupRegistrationSaveResult(
                Success: false,
                StartupStatusMessage: "기기 활성화 실패",
                StartupDetailMessage: BuildApiDetail(response),
                RegistrationMessage: $"기기 활성화에 실패했습니다.{Environment.NewLine}{BuildApiDetail(response)}");
        }

        var issuedApiKey = Normalize(response.ApiKey);
        if (string.IsNullOrWhiteSpace(issuedApiKey))
        {
            return new StartupRegistrationSaveResult(
                Success: false,
                StartupStatusMessage: "API 키 저장 실패",
                StartupDetailMessage: "활성화 응답에 API 키가 없습니다.",
                RegistrationMessage: "활성화 응답에서 API 키를 찾지 못했습니다.");
        }

        try
        {
            await _apiKeyStore.SaveAsync(issuedApiKey, cancellationToken);
            return new StartupRegistrationSaveResult(
                Success: true,
                StartupStatusMessage: "API 키 저장 완료",
                StartupDetailMessage: BuildApiDetail(response),
                RegistrationMessage: $"기기 활성화 후 API 키 저장을 완료했습니다.{Environment.NewLine}{BuildApiDetail(response)}");
        }
        catch (Exception ex)
        {
            return new StartupRegistrationSaveResult(
                Success: false,
                StartupStatusMessage: "API 키 저장 실패",
                StartupDetailMessage: ex.Message,
                RegistrationMessage: "API 키 저장에 실패했습니다. 권한 또는 경로를 확인하세요.");
        }
    }

    private async Task<string?> CheckApiKeyAsync(
        Action<StartupVerificationProgress>? onStageChanged,
        CancellationToken cancellationToken)
    {
        onStageChanged?.Invoke(new StartupVerificationProgress(
            StartupVerificationStage.CheckApiKey,
            StartupVerificationStageStatus.Running,
            "저장된 API 키를 확인 중입니다."));

        var apiKey = await _apiKeyStore.LoadAsync(cancellationToken);
        return Normalize(apiKey);
    }

    private async Task<DeviceApiResponse> VerifyDeviceAsync(
        string operationName,
        Func<CancellationToken, Task<DeviceApiResponse>> apiCall,
        Action<StartupVerificationProgress>? onStageChanged,
        CancellationToken cancellationToken)
    {
        onStageChanged?.Invoke(new StartupVerificationProgress(
            StartupVerificationStage.VerifyDevice,
            StartupVerificationStageStatus.Running,
            $"{operationName} API 호출 중..."));

        var response = await apiCall(cancellationToken);
        if (!response.Success)
        {
            onStageChanged?.Invoke(new StartupVerificationProgress(
                StartupVerificationStage.VerifyDevice,
                StartupVerificationStageStatus.Failed,
                response.Message));
        }

        return response;
    }

    private static StartupSequenceResult BuildFailure(
        StartupStateMachine stateMachine,
        string startupStatusMessage,
        string startupDetailMessage,
        string registrationMessage,
        bool canRegister,
        string registeredSerial = "-",
        string extractedSerial = "-")
    {
        return new StartupSequenceResult(
            FinalState: stateMachine.CurrentState,
            Transitions: stateMachine.Transitions,
            NextStep: StartupNextStep.ShowRegistration,
            StartupStatusMessage: startupStatusMessage,
            StartupDetailMessage: startupDetailMessage,
            RegisteredSerial: registeredSerial,
            ExtractedSerial: extractedSerial,
            RegistrationMessage: registrationMessage,
            CanRegister: canRegister);
    }

    private static string Normalize(string? value)
        => (value ?? string.Empty).Trim();

    private static string BuildApiDetail(DeviceApiResponse response)
    {
        var trace = string.IsNullOrWhiteSpace(response.Trace) ? response.Message : response.Trace;
        return $"{response.Message}{Environment.NewLine}{trace}";
    }
}
