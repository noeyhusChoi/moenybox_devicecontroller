using System;

namespace Kiosk.Infrastructure.Database.Ef.Entities;

public sealed class TransactionOutboxEntity
{
    public long Id { get; set; }
    public string KioskId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public string Status { get; set; } = "PENDING";
    public int RetryCount { get; set; }
    public DateTime NextRetryAt { get; set; }
    public DateTime? LastTriedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
