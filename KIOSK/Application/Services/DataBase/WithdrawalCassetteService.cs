using KIOSK.Infrastructure.Database;
using KIOSK.Infrastructure.Common.Utils;
using MySqlConnector;
using System.Data;

namespace KIOSK.Application.Services
{
    public readonly record struct WithdrawalCassette(string DeviceID, int Slot, string CurrencyCode, decimal Denomination, int Capacity, int Count);

    public sealed class WithdrawalCassetteService
    {
        private readonly IDatabaseService _db;
        private volatile HashSet<WithdrawalCassette> _withdrawalCassettes = new();

        public WithdrawalCassetteService(IDatabaseService db) => _db = db;

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            await LoadAsync(ct).ConfigureAwait(false);
        }

        public decimal GetTotalAmount(string currency)
        {
            return _withdrawalCassettes
                .Where(x => x.CurrencyCode == currency)
                .Sum(x => x.Denomination * x.Count);
        }

        public HashSet<WithdrawalCassette> Get() => _withdrawalCassettes;

        private async Task LoadAsync(CancellationToken ct)
        {
            try
            {
                const string sql = @"sp_get_cassette_info"; 

                var dataSet = await _db.QueryAsync<DataSet>(sql, type: CommandType.StoredProcedure);

                if (dataSet.Tables.Count < 1) return;

                var next = new HashSet<WithdrawalCassette>();
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    next.Add(new WithdrawalCassette()
                    {
                        DeviceID = row.Get<string>("DEVICE_ID"),
                        Slot = row.Get<int>("SLOT"),
                        CurrencyCode = row.Get<string>("CURRENCY_CODE"),
                        Denomination = row.Get<decimal>("DENOMINATION"),
                        Capacity = row.Get<int>("CAPACITY"),
                        Count = row.Get<int>("CURRENT_COUNT"),
                    });
                }

                // 교체형 캐시(락 없이 스레드-세이프 읽기)
                _withdrawalCassettes = next;
            }
            catch (Exception)
            {

            }
        }

        public async Task WithdrawalAsync(IEnumerable<(string deviceId, string currency_code, int slot, decimal denomination, int succeeded_count)> results, CancellationToken ct)
        {
            try
            {
                const string sql = @"sp_update_cassette_payout";

                foreach (var result in results)
                {
                    var res = await _db.QueryAsync<DataSet>(
                        sql,
                        new[]
                        {
                          DatabaseService.Param("@p_kiosk_id", MySqlDbType.VarChar, "C4E7..."),
                          DatabaseService.Param("@p_device_id", MySqlDbType.VarChar, result.deviceId),
                          DatabaseService.Param("@p_currency_code", MySqlDbType.VarChar, result.currency_code),
                          DatabaseService.Param("@p_slot", MySqlDbType.Int32, result.slot),
                          DatabaseService.Param("@p_denomination", MySqlDbType.Decimal, result.denomination),
                          DatabaseService.Param("@p_succeeded_count", MySqlDbType.Int32, result.succeeded_count)
                        },
                        CommandType.StoredProcedure);
                }
            }
            catch (Exception)
            {

            }
        }

        // TODO: 거래 결과인데 방출기에 있는 부분 어색함, 수정 필요
        public async Task ResultAsync(string json, CancellationToken ct = default)
        {
            const string sql = @"sp_save_tx_from_json";

            var res = await _db.QueryAsync<DataSet>(
                sql,
                new[]
                {
                   DatabaseService.Param("@p_tx", MySqlDbType.JSON, json)
                },
                CommandType.StoredProcedure);
        }
    }
}
