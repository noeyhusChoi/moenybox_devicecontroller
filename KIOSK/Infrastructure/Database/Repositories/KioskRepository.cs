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
    public class KioskRepository : IReadRepository<KioskModel>
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public KioskRepository(IDbContextFactory<KioskDbContext> contextFactory)
            => _contextFactory = contextFactory;

        public async Task<IReadOnlyList<KioskModel>> LoadAllAsync(CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var records = await context.Kiosks
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return records.Select(Map).ToList();
        }

        private static KioskModel Map(KioskInfoEntity record)
            => new KioskModel
            {
                Id = record.Id,
                Pid = record.Pid,
                IsValid = record.IsValid
            };
    }
}
