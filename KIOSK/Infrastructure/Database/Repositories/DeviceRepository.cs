using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Interface;
using KIOSK.Infrastructure.Database.Models;
using System.Linq;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public class DeviceRepository : RepositoryBase, IReadRepository<DeviceModel>
    {
        public DeviceRepository(IDatabaseService db) : base(db)
        {

        }

        public async Task<IReadOnlyList<DeviceModel>> LoadAllAsync(CancellationToken ct = default)
        {
            var records = await QueryAsync<DeviceRecord>("sp_get_device_info", null, ct);
            return records.Select(Map).ToList();
        }

        private static DeviceModel Map(DeviceRecord record)
            => new DeviceModel
            {
                Id = record.Id,
                Type = record.Type,
                CommType = record.CommType,
                CommPort = record.CommPort,
                CommParam = record.CommParam
            };
    }
}
