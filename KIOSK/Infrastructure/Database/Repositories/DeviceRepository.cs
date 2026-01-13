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
    public class DeviceRepository : IReadRepository<DeviceModel>
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public DeviceRepository(IDbContextFactory<KioskDbContext> contextFactory)
            => _contextFactory = contextFactory;

        public async Task<IReadOnlyList<DeviceModel>> LoadAllAsync(CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var records = await context.DeviceInstances
                .AsNoTracking()
                .Where(x => x.IsEnabled)
                .Include(x => x.Catalog)
                .Include(x => x.Comm)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return records.Select(Map).ToList();
        }

        private static DeviceModel Map(DeviceInstanceEntity record)
            => new DeviceModel
            {
                Id = record.DeviceId,
                Name = record.DeviceName,
                Vendor = record.Catalog?.Vendor ?? string.Empty,
                Model = record.Catalog?.Model ?? string.Empty,
                DriverType = record.Catalog?.DriverType ?? string.Empty,
                DeviceType = record.Catalog?.DeviceType ?? string.Empty,
                CommType = record.Comm?.CommType ?? string.Empty,
                CommPort = record.Comm?.CommPort ?? string.Empty,
                CommParam = record.Comm?.CommParams ?? string.Empty,
                PollingMs = record.Comm?.PollingMs ?? 0
            };
    }
}
