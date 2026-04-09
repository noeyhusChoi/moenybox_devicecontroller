namespace DeviceKit.Commands;

public enum CommandOrigin
{
    Auto,
    Manual
}

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

public sealed record DeviceCommandRecord(
    string Name,
    string Command,
    bool Success,
    ErrorCode? ErrorCode,
    CommandOrigin Origin,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    long DurationMs);
