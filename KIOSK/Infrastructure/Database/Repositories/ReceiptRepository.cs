using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Ef.Entities;
using KIOSK.Infrastructure.Database.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public class ReceiptRepository : IReadRepository<ReceiptModel>
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public ReceiptRepository(IDbContextFactory<KioskDbContext> contextFactory)
            => _contextFactory = contextFactory;

        public async Task<IReadOnlyList<ReceiptModel>> LoadAllAsync(CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var records = await context.Receipts
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return records.Select(Map).ToList();
        }

        private static ReceiptModel Map(ReceiptEntity record)
            => new ReceiptModel
            {
                Locale = record.Locale,
                Key = record.Key,
                Value = record.Value
            };
    }
}
