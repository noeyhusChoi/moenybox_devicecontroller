using System;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class ReceiptEntity
{
    public long Id { get; set; }
    public string KioskId { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
