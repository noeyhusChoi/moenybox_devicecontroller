using Kiosk.Infrastructure.Database.Ef;
using Kiosk.Infrastructure.Database.Ef.Entities;
using Kiosk.Infrastructure.Database.Interface;
using Kiosk.Infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kiosk.Infrastructure.Database.Repositories
{
    public class DepositCurrencyRepository : IReadRepository<DepositCurrencyModel>
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public DepositCurrencyRepository(IDbContextFactory<KioskDbContext> contextFactory)
            => _contextFactory = contextFactory;

        public async Task<IReadOnlyList<DepositCurrencyModel>> LoadAllAsync(CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var records = await context.DepositCurrencies
                .Where(x => x.IsValid)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return records.Select(Map).ToList();
        }

        public async Task<IReadOnlyList<DepositCurrencyModel>> LoadByKioskIdAsync(string kioskId, CancellationToken ct = default)
        {
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
