using System;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class WithdrawalCassetteEntity
{
    public string KioskId { get; set; } = string.Empty;
    public string DeviceID { get; set; } = string.Empty;
    public int Slot { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Denomination { get; set; }
    public int Capacity { get; set; }
    public int Count { get; set; }
    public bool IsValid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
