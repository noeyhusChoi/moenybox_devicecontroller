using System;

namespace Kiosk.Infrastructure.Database.Ef.Entities;

public sealed class DepositDenominationEntity
{
    public string KioskId { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Denomination { get; set; }
    public bool IsValid { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}
