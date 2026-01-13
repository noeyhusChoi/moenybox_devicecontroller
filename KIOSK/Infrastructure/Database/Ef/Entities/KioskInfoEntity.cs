using System;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class KioskInfoEntity
{
    public string Id { get; set; } = string.Empty;
    public string Pid { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
