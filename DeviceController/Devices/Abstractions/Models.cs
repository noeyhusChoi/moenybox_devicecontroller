// Abstractions/Models.cs
namespace KIOSK.Device.Abstractions
{
    public record DeviceCommand(string Name, object? Payload = null);

    public enum CommandOrigin { Auto, Manual }

    public readonly record struct CommandContext(
        CommandOrigin Origin,
        string? InitiatorId = null,
        string? Reason = null,
        string? CorrelationId = null)
    {
        public static CommandContext Manual(string? initiatorId = null, string? reason = null, string? correlationId = null)
            => new(CommandOrigin.Manual, initiatorId, reason, correlationId);

        public static CommandContext Auto(string? reason = null, string? correlationId = null)
            => new(CommandOrigin.Auto, null, reason, correlationId);
    }

    public record DeviceCommandRecord(
        string Name,
        string Command,
        bool Success,
        ErrorCode? ErrorCode,
        CommandOrigin Origin,
        DateTimeOffset StartedAt,
        DateTimeOffset FinishedAt,
        long DurationMs);

    public readonly record struct ErrorCode(string Domain, string Device, string Category, string Detail)
    {
        public override string ToString() => $"{Domain}.{Device}.{Category}.{Detail}";

        public static bool TryParse(string code, out ErrorCode result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var parts = code.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
                return false;

            result = new ErrorCode(parts[0], parts[1], parts[2], parts[3]);
            return true;
        }
    }

    public record CommandResult(
        bool Success,
        string Message = "",
        object? Data = null,
        ErrorCode? Code = null,
        bool Retryable = false); // 정책 적용 전까지는 힌트로만 사용됨

    //public record DeviceDescriptor(
    //    string kiosk_Id,
    //    string Model,
    //    string TransportName,   // ex) "COM3@115200"
    //    string ProtocolName,    // ex) "SimpleCRC"
    //    int PollingMs = 1000
    //);

    // 공통 열거형
    public enum DeviceHealth { Online, Offline }
    public enum Severity { Info, Warning, Error, Critical }

    // 알람/에러 항목(선택)
    public record StatusEvent(
        string Code,               // 예: "DEV.PRINTER.STATUS.NO_PAPER"
        string Message,            // 사용자 메시지
        Severity Severity,         // 중요도
        DateTimeOffset At,
        bool Notify = true,        // 외부 Notice 대상 여부
        ErrorCode? ErrorCode = null);

    // 단일 스냅샷(엔벨로프 + 유연 페이로드)
    public record StatusSnapshot()
    {
        public string Name { get; init; } = string.Empty;                     // 장치 식별자 (desc.Name)
        public string Model { get; init; } = string.Empty;                     // 모델 (desc.Model)
        public DeviceHealth Health { get; init; } = DeviceHealth.Offline;     // ← 추가
        public List<StatusEvent>? Alarms { get; init; } = new();              // 알람/에러 목록(선택)
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow; // 상태 시각(UTC 권장)

        public bool HasError => Alarms != null && Alarms.Count > 0 || Health == DeviceHealth.Offline;
    }
}
