// Abstractions/Models.cs
namespace KIOSK.Device.Abstractions
{
    public record DeviceCommand(string Name, object? Payload = null);
    public record CommandResult(bool Success, string Message = "", object? Data = null);

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
    public record DeviceAlarm(
        string Code,               // 예: "PAPER.OUT", "CUTTER.ERROR"
        string Message,            // 사용자 메시지
        Severity Severity,         // 중요도
        DateTimeOffset At
    );

    // 단일 스냅샷(엔벨로프 + 유연 페이로드)
    public record DeviceStatusSnapshot()
    {
        public string Name { get; init; } = string.Empty;                     // 장치 식별자 (desc.Name)
        public string Model { get; init; } = string.Empty;                     // 모델 (desc.Model)
        public DeviceHealth Health { get; init; } = DeviceHealth.Offline;     // ← 추가
        public List<DeviceAlarm>? Alarms { get; init; } = new();              // 알람/에러 목록(선택)
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow; // 상태 시각(UTC 권장)

        public bool HasError => Alarms != null && Alarms.Count > 0 || Health == DeviceHealth.Offline;
    }
}
