using KIOSK.Infrastructure.Database;

namespace KIOSK.Application.Services.Health
{
    public sealed class DatabaseHealthService : IDatabaseHealthService
    {
        private readonly IDatabaseService _db;

        public DatabaseHealthService(IDatabaseService db)
        {
            _db = db;
        }

        public Task<bool> CanConnectAsync(CancellationToken ct = default)
            => _db.CanConnectAsync();
    }
}
