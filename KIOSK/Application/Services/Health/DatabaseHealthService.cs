using KIOSK.Infrastructure.Database.Ef;
using Microsoft.EntityFrameworkCore;

namespace KIOSK.Application.Services.Health
{
    public sealed class DatabaseHealthService : IDatabaseHealthService
    {
        private readonly IDbContextFactory<KioskDbContext> _dbContextFactory;

        public DatabaseHealthService(IDbContextFactory<KioskDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<bool> CanConnectAsync(CancellationToken ct = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            return await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
        }
    }
}
