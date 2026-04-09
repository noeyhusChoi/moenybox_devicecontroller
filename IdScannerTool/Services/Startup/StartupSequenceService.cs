namespace IdScannerTool.Services;

public sealed class StartupSequenceService : IStartupSequenceService
{
    private readonly ISerialRegistrationStateService _serialRegistrationStateService;
    private readonly IStartupVerificationService _startupVerificationService;

    public StartupSequenceService(
        ISerialRegistrationStateService serialRegistrationStateService,
        IStartupVerificationService startupVerificationService)
    {
        _serialRegistrationStateService = serialRegistrationStateService;
        _startupVerificationService = startupVerificationService;
    }

    public async Task<StartupSequenceResult> RunStartupAsync(
        Action<StartupVerificationProgress>? onStageChanged = null,
        CancellationToken cancellationToken = default)
    {
        var stateMachine = new StartupStateMachine();
        stateMachine.MoveToVerifyingSerial("장치 연결 및 시리얼 추출 시작");

        StartupRegistrationExtractionResult extraction;
        try
        {
            extraction = await _startupVerificationService.ExtractForRegistrationAsync(
                onStageChanged,
                cancellationToken);
        }
        catch (Exception ex)
        {
            stateMachine.MoveToFailed("장치 연결/시리얼 추출 실패");
            return new StartupSequenceResult(
                FinalState: stateMachine.CurrentState,
                Transitions: stateMachine.Transitions,
                NextStep: StartupNextStep.ShowRegistration,
                StartupStatusMessage: "장치 확인 필요",
                StartupDetailMessage: ex.Message,
                RegisteredSerial: "-",
                ExtractedSerial: "-",
                RegistrationMessage: $"오류: {ex.Message}",
                CanRegister: false);
        }

        var extractedSerial = Normalize(extraction.ExtractedSerial);
        if (!extraction.Success || string.IsNullOrWhiteSpace(extractedSerial))
        {
            stateMachine.MoveToFailed("장치 연결/시리얼 추출 실패");
            return new StartupSequenceResult(
                FinalState: stateMachine.CurrentState,
                Transitions: stateMachine.Transitions,
                NextStep: StartupNextStep.ShowRegistration,
                StartupStatusMessage: "장치 확인 필요",
                StartupDetailMessage: extraction.StartupDetailMessage,
                RegisteredSerial: "-",
                ExtractedSerial: "-",
                RegistrationMessage: "장치 연결 또는 시리얼 추출에 실패했습니다. 장치를 확인하세요.",
                CanRegister: false);
        }

        stateMachine.MoveToCheckingLocalSerial("로컬 시리얼 등록 상태 확인");
        LocalSerialRegistrationState registrationState;
        try
        {
            registrationState = await _serialRegistrationStateService.GetStateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            stateMachine.MoveToFailed("로컬 시리얼키 확인 실패");
            return new StartupSequenceResult(
                FinalState: stateMachine.CurrentState,
                Transitions: stateMachine.Transitions,
                NextStep: StartupNextStep.ShowRegistration,
                StartupStatusMessage: "로컬 시리얼키 확인 실패",
                StartupDetailMessage: ex.Message,
                RegisteredSerial: "-",
                ExtractedSerial: extractedSerial,
                RegistrationMessage: $"오류: {ex.Message}",
                CanRegister: false);
        }

        if (registrationState.IsRegistered)
        {
            stateMachine.MoveToVerifyingSerial("등록 상태: 시리얼 매칭");
            bool matched;
            try
            {
                matched = await _startupVerificationService.CompareSerialAsync(
                    registrationState.RegisteredSerial,
                    extractedSerial,
                    onStageChanged,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                stateMachine.MoveToFailed("시리얼 비교 실패");
                return new StartupSequenceResult(
                    FinalState: stateMachine.CurrentState,
                    Transitions: stateMachine.Transitions,
                    NextStep: StartupNextStep.ShowRegistration,
                    StartupStatusMessage: "시리얼 비교 실패",
                    StartupDetailMessage: ex.Message,
                    RegisteredSerial: registrationState.RegisteredSerial,
                    ExtractedSerial: extractedSerial,
                    RegistrationMessage: $"오류: {ex.Message}",
                    CanRegister: false);
            }

            if (!matched)
            {
                stateMachine.MoveToFailed("등록 상태: 시리얼 불일치");
                return new StartupSequenceResult(
                    FinalState: stateMachine.CurrentState,
                    Transitions: stateMachine.Transitions,
                    NextStep: StartupNextStep.ShowRegistration,
                    StartupStatusMessage: "시리얼 불일치",
                    StartupDetailMessage: $"local={registrationState.RegisteredSerial}, device={extractedSerial}",
                    RegisteredSerial: registrationState.RegisteredSerial,
                    ExtractedSerial: extractedSerial,
                    RegistrationMessage: $"시리얼 불일치. local={registrationState.RegisteredSerial}, device={extractedSerial}. 확인 후 재시도하세요.",
                    CanRegister: false);
            }

            stateMachine.MoveToReady("시리얼 매칭 성공");
            return new StartupSequenceResult(
                FinalState: stateMachine.CurrentState,
                Transitions: stateMachine.Transitions,
                NextStep: StartupNextStep.ShowMain,
                StartupStatusMessage: "시리얼 검증 완료",
                StartupDetailMessage: $"local={registrationState.RegisteredSerial}, device={extractedSerial}",
                RegisteredSerial: registrationState.RegisteredSerial,
                ExtractedSerial: extractedSerial,
                RegistrationMessage: "검증 완료",
                CanRegister: false);
        }

        stateMachine.MoveToVerifyingSerial("로컬 미등록: 서버 인증");
        var serverAuth = await AuthenticateSerialWithServerAsync(extractedSerial, cancellationToken);
        if (!serverAuth.Success)
        {
            stateMachine.MoveToFailed("로컬 미등록: 서버 인증 실패");
            return new StartupSequenceResult(
                FinalState: stateMachine.CurrentState,
                Transitions: stateMachine.Transitions,
                NextStep: StartupNextStep.ShowRegistration,
                StartupStatusMessage: "서버 인증 실패",
                StartupDetailMessage: serverAuth.Message,
                RegisteredSerial: "-",
                ExtractedSerial: extractedSerial,
                RegistrationMessage: "서버 인증 실패. 장치를 확인하세요.",
                CanRegister: false);
        }

        var saveResult = await _serialRegistrationStateService.SaveAsync(extractedSerial, cancellationToken);
        if (!saveResult.Success)
        {
            stateMachine.MoveToFailed("서버 인증 후 로컬 시리얼 저장 실패");
            return new StartupSequenceResult(
                FinalState: stateMachine.CurrentState,
                Transitions: stateMachine.Transitions,
                NextStep: StartupNextStep.ShowRegistration,
                StartupStatusMessage: saveResult.StartupStatusMessage,
                StartupDetailMessage: saveResult.StartupDetailMessage,
                RegisteredSerial: "-",
                ExtractedSerial: extractedSerial,
                RegistrationMessage: saveResult.RegistrationMessage,
                CanRegister: true);
        }

        stateMachine.MoveToReady("서버 인증 성공 및 시리얼 등록 완료");
        return new StartupSequenceResult(
            FinalState: stateMachine.CurrentState,
            Transitions: stateMachine.Transitions,
            NextStep: StartupNextStep.ShowMain,
            StartupStatusMessage: "시리얼 등록 및 검증 완료",
            StartupDetailMessage: $"saved={extractedSerial}",
            RegisteredSerial: extractedSerial,
            ExtractedSerial: extractedSerial,
            RegistrationMessage: "서버 인증 성공. 시리얼 등록 후 메인 화면으로 이동합니다.",
            CanRegister: false);
    }

    public Task<StartupRegistrationExtractionResult> ExtractForRegistrationAsync(
        CancellationToken cancellationToken = default)
        => _startupVerificationService.ExtractForRegistrationAsync(cancellationToken: cancellationToken);

    public Task<StartupRegistrationSaveResult> SaveRegistrationAsync(
        string serial,
        CancellationToken cancellationToken = default)
        => _serialRegistrationStateService.SaveAsync(serial, cancellationToken);

    private static string Normalize(string? serial)
        => (serial ?? string.Empty).Trim().ToUpperInvariant();

    private static Task<(bool Success, string Message)> AuthenticateSerialWithServerAsync(
        string extractedSerial,
        CancellationToken cancellationToken)
    {
        _ = extractedSerial;
        _ = cancellationToken;
        // TODO: 서버 인증 API 연동 전까지 임시 성공 처리
        return Task.FromResult((true, "서버 인증 성공(임시 처리)"));
    }
}
