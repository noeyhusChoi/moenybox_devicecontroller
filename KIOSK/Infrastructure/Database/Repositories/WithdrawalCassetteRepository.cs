using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Interface;
using KIOSK.Infrastructure.Database.Models;
using MySqlConnector;
using System.Linq;
using System.Text.Json;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public class WithdrawalCassetteRepository : RepositoryBase, IReadRepository<WithdrawalCassetteModel>, IUpdateRepository<WithdrawalCassetteModel>
    {
        public WithdrawalCassetteRepository(IDatabaseService db) : base(db)
        {
        }

        public async Task<IReadOnlyList<WithdrawalCassetteModel>> LoadAllAsync(CancellationToken ct = default)
        {
            var records = await QueryAsync<WithdrawalCassetteRecord>("sp_get_cassette_info", null, ct);
            return records.Select(Map).ToList();
        }

        public Task UpdateAsync(IReadOnlyList<WithdrawalCassetteModel> entities, CancellationToken ct = default)
        {
            //if (entities == null || entities.Count == 0)
            //    throw new ArgumentException("카세트 슬롯 정보가 없습니다.", nameof(entities));

            var json = JsonSerializer.Serialize(entities);

            var parameters = new[]
            {
                // TODO: ERROR 찾아서 수정
                DatabaseService.Param("@p_slots_json",MySqlDbType.JSON, json )
            };

            return ExecAsync("sp_withdrawal_cassette_upsert", parameters, ct);
        }

        private static WithdrawalCassetteModel Map(WithdrawalCassetteRecord record)
            => new WithdrawalCassetteModel
            {
                DeviceID = record.DeviceID,
                DeviceName = record.DeviceName,
                Slot = record.Slot,
                CurrencyCode = record.CurrencyCode,
                Denomination = record.Denomination,
                Capacity = record.Capacity,
                Count = record.Count
            };
    }
}
