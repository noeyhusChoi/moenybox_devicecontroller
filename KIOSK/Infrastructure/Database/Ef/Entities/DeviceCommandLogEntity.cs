using System;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class DeviceCommandLogEntity
{
    public long Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string CommandName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string Origin { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public long DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
