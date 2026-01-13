using System;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class DeviceStatusLogEntity
{
    public long Id { get; set; }
    public string KioskId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
