using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Interface;
using KIOSK.Infrastructure.Database.Models;
using System.Linq;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public  class DepositCurrencyRepository : RepositoryBase, IReadRepository<DepositCurrencyModel>
    {
        public DepositCurrencyRepository(IDatabaseService db) : base(db)
        {

        }

        public async Task<IReadOnlyList<DepositCurrencyModel>> LoadAllAsync(CancellationToken ct = default)
        {
            var records = await QueryAsync<DepositCurrencyRecord>("sp_get_deposit_attribute_info", null, ct);
            return records.Select(Map).ToList();
        }

        private static DepositCurrencyModel Map(DepositCurrencyRecord record)
            => new DepositCurrencyModel
            {
                CurrencyCode = record.CurrencyCode,
                Denomination = record.Denomination,
                AttributeCode = record.AttributeCode
            };
    }
}
