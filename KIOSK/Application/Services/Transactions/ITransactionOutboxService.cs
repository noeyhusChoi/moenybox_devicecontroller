namespace KIOSK.Application.Services.Transactions
{
    public interface ITransactionOutboxService
    {
        Task MarkSuccessAsync(string transactionId, CancellationToken ct = default);
        Task MarkFailAsync(string transactionId, CancellationToken ct = default);
    }
}
