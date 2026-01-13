using System;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class DepositCurrencyEntity
{
    public long Id { get; set; }
    public string KioskId { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Denomination { get; set; }
    public string AttributeCode { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
