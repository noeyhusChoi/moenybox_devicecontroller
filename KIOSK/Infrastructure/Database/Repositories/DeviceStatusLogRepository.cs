using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Ef.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public sealed class DeviceStatusLogRepository
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public DeviceStatusLogRepository(IDbContextFactory<KioskDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task SaveAsync(string kioskId, string deviceType, string name, StatusSnapshot snapshot, CancellationToken ct = default)
        {
            if (snapshot.Alerts is null || snapshot.Alerts.Count == 0)
                return;
            if (string.IsNullOrWhiteSpace(kioskId))
                return;
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
