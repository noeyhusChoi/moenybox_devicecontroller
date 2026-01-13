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
    public class ApiConfigRepository : IReadRepository<ApiConfigModel>
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public ApiConfigRepository(IDbContextFactory<KioskDbContext> contextFactory)
            => _contextFactory = contextFactory;

        public async Task<IReadOnlyList<ApiConfigModel>> LoadAllAsync(CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var records = await context.ApiConfigs
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return records.Select(Map).ToList();
        }

        private static ApiConfigModel Map(ApiConfigEntity record)
            => new ApiConfigModel
            {
                KioskId = record.KioskId ?? string.Empty,
                ServerName = record.ServerName ?? string.Empty,
                ServerUrl = record.ServerUrl ?? string.Empty,
                ServerKey = record.ServerKey ?? string.Empty,
                TimeoutSeconds = record.TimeoutSeconds ?? 0,
                IsValid = record.IsValid
            };
    }
}
