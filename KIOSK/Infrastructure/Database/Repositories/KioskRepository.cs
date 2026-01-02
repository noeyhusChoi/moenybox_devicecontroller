using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Interface;
using KIOSK.Infrastructure.Database.Models;
using System.Linq;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public class KioskRepository : RepositoryBase, IReadRepository<KioskModel>
    {
        public KioskRepository(IDatabaseService db) : base(db)
        { }

        public async Task<IReadOnlyList<KioskModel>> LoadAllAsync(CancellationToken ct = default)
        {
            var records = await QueryAsync<KioskRecord>("sp_get_kiosk_info", null, ct);
            return records.Select(Map).ToList();
        }

        private static KioskModel Map(KioskRecord record)
            => new KioskModel
            {
                Id = record.Id,
                Pid = record.Pid
            };
    }
}
