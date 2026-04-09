namespace Kiosk.Infrastructure.Database.Ef.Entities;

public sealed class TransactionOutboxRow
{
    public long TransactionId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
}
