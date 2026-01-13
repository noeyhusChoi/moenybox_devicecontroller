using System;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class DeviceInstanceEntity
{
    public string DeviceId { get; set; } = string.Empty;
    public string KioskId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public long CatalogId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DeviceCatalogEntity? Catalog { get; set; }
    public DeviceCommEntity? Comm { get; set; }
}
