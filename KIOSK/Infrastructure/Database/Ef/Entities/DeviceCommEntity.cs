using System;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class DeviceCommEntity
{
    public string DeviceId { get; set; } = string.Empty;
    public string CommType { get; set; } = string.Empty;
    public string CommPort { get; set; } = string.Empty;
    public string CommParams { get; set; } = string.Empty;
    public int PollingMs { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DeviceInstanceEntity? Device { get; set; }
}
