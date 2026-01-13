using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Ef.Entities;
using KIOSK.Infrastructure.Database.Interface;
using Microsoft.EntityFrameworkCore;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public sealed class DeviceCommandLogRepository
        : ICreateRepository<DeviceCommandRecord>
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public DeviceCommandLogRepository(IDbContextFactory<KioskDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public Task SaveAsync(DeviceCommandRecord record)
            => InsertAsync(record);

        public async Task InsertAsync(DeviceCommandRecord record, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var entry = new DeviceCommandLogEntity
            {
                DeviceName = record.Name,
                CommandName = record.Command,
                Success = record.Success,
                ErrorCode = record.ErrorCode?.ToString(),
                Origin = record.Origin.ToString(),
                StartedAt = record.StartedAt.UtcDateTime,
                FinishedAt = record.FinishedAt.UtcDateTime,
                DurationMs = record.DurationMs
            };
            context.DeviceCommandLogs.Add(entry);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
