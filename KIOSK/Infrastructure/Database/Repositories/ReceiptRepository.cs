using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Interface;
using KIOSK.Infrastructure.Database.Models;
using System.Linq;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public class ReceiptRepository : RepositoryBase, IReadRepository<ReceiptModel>
    {
        public ReceiptRepository(IDatabaseService db) : base(db)
        {
        }

        public async Task<IReadOnlyList<ReceiptModel>> LoadAllAsync(CancellationToken ct = default)
        {
            var records = await QueryAsync<ReceiptRecord>("sp_get_receipt_info", null, ct);
            return records.Select(Map).ToList();
        }

        private static ReceiptModel Map(ReceiptRecord record)
            => new ReceiptModel
            {
                Locale = record.Locale,
                Key = record.Key,
                Value = record.Value
            };
    }
}
