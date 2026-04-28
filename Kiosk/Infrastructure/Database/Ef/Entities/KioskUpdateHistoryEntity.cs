using System;

namespace Kiosk.Infrastructure.Database.Ef.Entities;

public sealed class KioskUpdateHistoryEntity
{
    public long Id { get; set; }
    public string KioskId { get; set; } = string.Empty;
    public int UpdateNo { get; set; }
    public string UpdateSource { get; set; } = string.Empty;
    public DateTime UpdateDateTime { get; set; }
}
