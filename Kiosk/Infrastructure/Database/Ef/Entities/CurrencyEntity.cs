using System;

namespace Kiosk.Infrastructure.Database.Ef.Entities;

public sealed class CurrencyEntity
{
    public string KioskId { get; set; } = string.Empty;
    public string CultureCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public int CurrencyDecimal { get; set; }
    public string CurrencySymbol { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
