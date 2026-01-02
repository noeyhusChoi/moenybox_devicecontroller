using KIOSK.Infrastructure.Database.Interface;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Models;
using System.Linq;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public  class ApiConfigRepository : RepositoryBase, IReadRepository<ApiConfigModel>
    {
        public ApiConfigRepository(IDatabaseService db) : base(db)
        {

        }

        public async Task<IReadOnlyList<ApiConfigModel>> LoadAllAsync(CancellationToken ct = default)
        {
            var records = await QueryAsync<ApiConfigRecord>("sp_get_server_info", null, ct);
            return records.Select(Map).ToList();
        }

        private static ApiConfigModel Map(ApiConfigRecord record)
            => new ApiConfigModel
            {
                ServerName = record.ServerName,
                ServerUrl = record.ServerUrl,
                ServerKey = record.ServerKey,
                TimeoutSeconds = record.TimeoutSeconds
            };
    }
}
