using Kiosk.Infrastructure.Database.Ef;
using Kiosk.Infrastructure.Database.Ef.Entities;
using Kiosk.Infrastructure.Common.Utils;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Kiosk.Application.Services
{
    public readonly record struct WithdrawalCassette(string DeviceID, int Slot, string CurrencyCode, decimal Denomination, int Capacity, int Count);

    public sealed class WithdrawalCassetteService
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;
        private volatile HashSet<WithdrawalCassette> _withdrawalCassettes = new();

        public WithdrawalCassetteService(IDbContextFactory<KioskDbContext> contextFactory) => _contextFactory = contextFactory;

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
                await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                var records = await context.WithdrawalCassettes
                    .FromSqlRaw("CALL sp_get_cassette_info")
                    .AsNoTracking()
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                if (records.Count == 0)
                    return;

                var next = new HashSet<WithdrawalCassette>(records.Count);
                foreach (var record in records)
                {
                    next.Add(new WithdrawalCassette()
                    {
                        DeviceID = record.DeviceID,
                        Slot = record.Slot,
                        CurrencyCode = record.CurrencyCode,
                        Denomination = record.Denomination,
                        Capacity = record.Capacity,
                        Count = record.Count,
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
                const string sql = @"CALL sp_update_cassette_payout(@p_kiosk_id, @p_device_id, @p_currency_code, @p_slot, @p_denomination, @p_succeeded_count)";

                foreach (var result in results)
                {
                    var parameters = new[]
                    {
                        new MySqlParameter("@p_kiosk_id", MySqlDbType.VarChar) { Value = "C4E7..." },
                        new MySqlParameter("@p_device_id", MySqlDbType.VarChar) { Value = result.deviceId },
                        new MySqlParameter("@p_currency_code", MySqlDbType.VarChar) { Value = result.currency_code },
                        new MySqlParameter("@p_slot", MySqlDbType.Int32) { Value = result.slot },
                        new MySqlParameter("@p_denomination", MySqlDbType.Decimal) { Value = result.denomination },
                        new MySqlParameter("@p_succeeded_count", MySqlDbType.Int32) { Value = result.succeeded_count }
                    };

                    await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                    await context.Database.ExecuteSqlRawAsync(sql, parameters, ct).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {

            }
        }

        // TODO: 거래 결과인데 방출기에 있는 부분 어색함, 수정 필요
        public async Task ResultAsync(string json, CancellationToken ct = default)
        {
            const string sql = @"CALL sp_save_tx_from_json(@p_tx)";
            var parameters = new[]
            {
                new MySqlParameter("@p_tx", MySqlDbType.JSON) { Value = json }
            };

            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            await context.Database.ExecuteSqlRawAsync(sql, parameters, ct).ConfigureAwait(false);
        }
    }
}
