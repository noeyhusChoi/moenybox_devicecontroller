namespace IdScannerTool.Services;

public sealed class SerialRegistrationStateService : ISerialRegistrationStateService
{
    private readonly ILocalSerialKeyStore _serialKeyStore;

    public SerialRegistrationStateService(ILocalSerialKeyStore serialKeyStore)
    {
        _serialKeyStore = serialKeyStore;
    }

    public async Task<LocalSerialRegistrationState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var serial = Normalize(await _serialKeyStore.LoadAsync(cancellationToken));
        var isRegistered = !string.IsNullOrWhiteSpace(serial);
        return new LocalSerialRegistrationState(
            IsRegistered: isRegistered,
            RegisteredSerial: isRegistered ? serial : "-");
    }

    public async Task<StartupRegistrationSaveResult> SaveAsync(string serial, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(serial);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new StartupRegistrationSaveResult(
                Success: false,
                StartupStatusMessage: "로컬 시리얼 저장 실패",
                StartupDetailMessage: "유효하지 않은 시리얼",
                RegistrationMessage: "저장할 시리얼 값이 비어 있습니다.");
        }

        try
        {
            await _serialKeyStore.SaveAsync(normalized, cancellationToken);
            return new StartupRegistrationSaveResult(
                Success: true,
                StartupStatusMessage: "로컬 시리얼 저장 완료",
                StartupDetailMessage: $"saved={normalized}",
                RegistrationMessage: "로컬 시리얼 저장 완료. 검증을 다시 시작합니다.");
        }
        catch
        {
            return new StartupRegistrationSaveResult(
                Success: false,
                StartupStatusMessage: "로컬 시리얼 저장 실패",
                StartupDetailMessage: "파일 저장 오류",
                RegistrationMessage: "시리얼 저장 실패. 권한 또는 경로를 확인하세요.");
        }
    }

    private static string Normalize(string? serial)
        => (serial ?? string.Empty).Trim().ToUpperInvariant();
}
