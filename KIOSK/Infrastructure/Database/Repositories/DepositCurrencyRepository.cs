using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Ef.Entities;
using KIOSK.Infrastructure.Database.Interface;
using KIOSK.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public class DepositCurrencyRepository : IReadRepository<DepositCurrencyModel>
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;
        private readonly IMemoryCache _cache;

        public DepositCurrencyRepository(IDbContextFactory<KioskDbContext> contextFactory, IMemoryCache cache)
        {
            _contextFactory = contextFactory;
            _cache = cache;
        }

        public async Task<IReadOnlyList<DepositCurrencyModel>> LoadAllAsync(CancellationToken ct = default)
        {
            var kiosks = _cache.Get<IReadOnlyList<KioskModel>>(DatabaseCacheKeys.Kiosk)
                ?? Array.Empty<KioskModel>();
            var kioskId = kiosks.FirstOrDefault()?.Id;
            if (string.IsNullOrWhiteSpace(kioskId))
                return Array.Empty<DepositCurrencyModel>();

            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var records = await context.DepositCurrencies
                .Where(x => x.KioskId == kioskId && x.IsValid)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return records.Select(Map).ToList();
        }

        private static DepositCurrencyModel Map(DepositCurrencyEntity record)
            => new DepositCurrencyModel
            {
                CurrencyCode = record.CurrencyCode,
                Denomination = record.Denomination,
                AttributeCode = record.AttributeCode
            };
    }
}
