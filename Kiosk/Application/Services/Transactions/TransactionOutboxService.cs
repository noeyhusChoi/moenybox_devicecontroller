using Kiosk.Infrastructure.Database.Ef;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kiosk.Application.Services.Transactions
{
    public sealed class TransactionOutboxService : ITransactionOutboxService
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public TransactionOutboxService(IDbContextFactory<KioskDbContext> contextFactory)
            => _contextFactory = contextFactory;

        public async Task MarkSuccessAsync(string transactionId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var row = await context.TransactionOutboxes
                .SingleOrDefaultAsync(x => x.TransactionId == transactionId, ct)
                .ConfigureAwait(false);
            if (row is null)
                return;

            row.Status = "SENT";
            row.LastTriedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        public async Task MarkFailAsync(string transactionId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var row = await context.TransactionOutboxes
                .SingleOrDefaultAsync(x => x.TransactionId == transactionId, ct)
                .ConfigureAwait(false);
            if (row is null)
                return;

            row.Status = "FAILED";
            row.RetryCount += 1;
            row.LastTriedAt = DateTime.UtcNow;
            row.NextRetryAt = DateTime.UtcNow.AddMinutes(1);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
