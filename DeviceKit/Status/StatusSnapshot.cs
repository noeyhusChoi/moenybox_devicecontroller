namespace DeviceKit.Status;

public enum DeviceConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Faulted = 3,
}

public enum Severity
{
    Info,
    Warning,
    Error,
    Critical
}

public sealed record StatusEvent(
    string Code,
    string Message,
    Severity Severity,
    DateTimeOffset At,
    bool Notify = true,
    ErrorCode? ErrorCode = null);

public sealed record DeviceConnectionSnapshot
{
    public string DeviceId { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public DeviceConnectionState State { get; init; } = DeviceConnectionState.Disconnected;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record StatusSnapshot
{
    public string DeviceId { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public List<StatusEvent> Alerts { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public bool IsHealthy => Alerts.Count == 0;
}
