using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Cache;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Ef.Entities;
using KIOSK.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public sealed class DeviceStatusLogRepository
    {
        private readonly IMemoryCache _cache;
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public DeviceStatusLogRepository(IDbContextFactory<KioskDbContext> contextFactory, IMemoryCache cache)
        {
            _cache = cache;
            _contextFactory = contextFactory;
        }

        public async Task SaveAsync(string name, StatusSnapshot snapshot, CancellationToken ct = default)
        {
            if (snapshot.Alerts is null || snapshot.Alerts.Count == 0)
                return;

            var kiosks = _cache.Get<IReadOnlyList<KioskModel>>(DatabaseCacheKeys.Kiosk)
                ?? Array.Empty<KioskModel>();
            var kioskId = kiosks.FirstOrDefault()?.Id;
            if (string.IsNullOrWhiteSpace(kioskId))
                return;

            var devices = _cache.Get<IReadOnlyList<DeviceModel>>(DatabaseCacheKeys.DeviceList)
                ?? Array.Empty<DeviceModel>();
            var deviceType = devices
                .FirstOrDefault(d =>
                    string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.Id, name, StringComparison.OrdinalIgnoreCase))
                ?.DeviceType;
            if (string.IsNullOrWhiteSpace(deviceType))
                return;

            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

            var entries = new List<DeviceStatusLogEntity>(snapshot.Alerts.Count);
            foreach (var alert in snapshot.Alerts)
            {
                entries.Add(new DeviceStatusLogEntity
                {
                    KioskId = kioskId,
                    DeviceName = name,
                    DeviceType = deviceType,
                    Source = alert.Source.ToString(),
                    Code = alert.ErrorCode?.ToString() ?? alert.Code,
                    Severity = alert.Severity.ToString(),
                    Message = alert.Message,
                    CreatedAt = alert.At.UtcDateTime
                });
            }

            context.DeviceStatusLogs.AddRange(entries);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
