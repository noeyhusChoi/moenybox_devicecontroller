using KIOSK.Infrastructure.Database.Ef;
using Microsoft.EntityFrameworkCore;

namespace KIOSK.Application.Services.Transactions
{
    public sealed class TransactionOutboxService : ITransactionOutboxService
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public TransactionOutboxService(IDbContextFactory<KioskDbContext> contextFactory)
            => _contextFactory = contextFactory;

        public async Task MarkSuccessAsync(string transactionId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            await context.Database.ExecuteSqlRawAsync(
                "CALL sp_update_tx_outbox_success({0})",
                new object[] { transactionId },
                ct).ConfigureAwait(false);
        }

        public async Task MarkFailAsync(string transactionId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            await context.Database.ExecuteSqlRawAsync(
                "CALL sp_update_tx_outbox_fail({0})",
                new object[] { transactionId },
                ct).ConfigureAwait(false);
        }
    }
}
