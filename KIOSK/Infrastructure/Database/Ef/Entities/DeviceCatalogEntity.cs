using System;
using System.Collections.Generic;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class DeviceCatalogEntity
{
    public long CatalogId { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string DriverType { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<DeviceInstanceEntity> Instances { get; set; } = new List<DeviceInstanceEntity>();
}
