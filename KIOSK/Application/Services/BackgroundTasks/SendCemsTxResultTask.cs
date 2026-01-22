using KIOSK.Application.Services.API;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Common.Utils;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Ef.Entities;
using KIOSK.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KIOSK.Application.Services.BackgroundTasks
{
    /// <summary>
    /// CEMS 거래 결과 전송 백그라운드 작업.
    /// </summary>
    public sealed class SendCemsTxResultTask
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;
        private readonly CemsApiService _cemsApi;
        private readonly ILoggingService _logger;

        public SendCemsTxResultTask(IDbContextFactory<KioskDbContext> contextFactory, CemsApiService cemsApi, ILoggingService logger)
        {
            _contextFactory = contextFactory;
            _cemsApi = cemsApi;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var rows = await context.TransactionOutboxRows
                .FromSqlRaw("CALL sp_get_tx_outbox")
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            if (rows.Count == 0)
                return;

            foreach (var row in rows)
            {
                var json = row.PayloadJson ?? string.Empty;
                var transaction = JsonConvertExtension.ConvertFromJson<TransactionModelV2>(json);

                var res = await _cemsApi.RegisterTransactionAsync(transaction, ct);

                var proc = res.Result && res.ECode == null
                    ? "CALL sp_update_tx_outbox_success({0})"
                    : "CALL sp_update_tx_outbox_fail({0})";

                await context.Database.ExecuteSqlRawAsync(
                    proc,
                    new object[] { transaction.TransactionID },
                    ct).ConfigureAwait(false);
            }
        }
    }
}
