using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Ef.Entities;
using KIOSK.Infrastructure.Database.Interface;
using KIOSK.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public class WithdrawalCassetteRepository : IReadRepository<WithdrawalCassetteModel>, IUpdateRepository<WithdrawalCassetteModel>
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;
        private readonly IMemoryCache _cache;

        public WithdrawalCassetteRepository(IDbContextFactory<KioskDbContext> contextFactory, IMemoryCache cache)
        {
            _contextFactory = contextFactory;
            _cache = cache;
        }

        public async Task<IReadOnlyList<WithdrawalCassetteModel>> LoadAllAsync(CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var records = await context.WithdrawalCassettes
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return records.Select(Map).ToList();
        }

        public async Task UpdateAsync(IReadOnlyList<WithdrawalCassetteModel> entities, CancellationToken ct = default)
        {
            if (entities == null || entities.Count == 0)
                return;

            var kiosks = _cache.Get<IReadOnlyList<KioskModel>>(DatabaseCacheKeys.Kiosk)
                ?? Array.Empty<KioskModel>();
            var kioskId = kiosks.FirstOrDefault()?.Id;
            if (string.IsNullOrWhiteSpace(kioskId))
                return;

            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var existing = await context.WithdrawalCassettes
                .Where(x => x.KioskId == kioskId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var map = existing.ToDictionary(x => (x.KioskId, x.DeviceID, x.Slot));

            foreach (var model in entities)
            {
                var modelKioskId = string.IsNullOrWhiteSpace(model.KioskId) ? kioskId : model.KioskId;
                var key = (modelKioskId, model.DeviceID, model.Slot);
                if (map.TryGetValue(key, out var entity))
                {
                    entity.CurrencyCode = model.CurrencyCode;
                    entity.Denomination = model.Denomination;
                    entity.Capacity = model.Capacity;
                    entity.Count = model.Count;
                    entity.IsValid = model.IsValid;
                }
                else
                {
                    context.WithdrawalCassettes.Add(new WithdrawalCassetteEntity
                    {
                        KioskId = modelKioskId,
                        DeviceID = model.DeviceID,
                        Slot = model.Slot,
                        CurrencyCode = model.CurrencyCode,
                        Denomination = model.Denomination,
                        Capacity = model.Capacity,
                        Count = model.Count,
                        IsValid = model.IsValid
                    });
                }
            }

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private static WithdrawalCassetteModel Map(WithdrawalCassetteEntity record)
            => new WithdrawalCassetteModel
            {
                KioskId = record.KioskId,
                DeviceID = record.DeviceID,
                DeviceName = string.Empty,
                Slot = record.Slot,
                CurrencyCode = record.CurrencyCode,
                Denomination = record.Denomination,
                Capacity = record.Capacity,
                Count = record.Count,
                IsValid = record.IsValid
            };
    }
}
