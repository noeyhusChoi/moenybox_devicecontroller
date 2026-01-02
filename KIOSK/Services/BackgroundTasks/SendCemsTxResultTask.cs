using KIOSK.Infrastructure.Database;
using KIOSK.Infrastructure.Logging;
using KIOSK.Models;
using KIOSK.Services.API;
using KIOSK.Utils;
using MySqlConnector;
using System.Data;

namespace KIOSK.Services.BackgroundTasks
{
    /// <summary>
    /// CEMS 거래 결과 전송 백그라운드 작업.
    /// </summary>
    public sealed class SendCemsTxResultTask
    {
        private readonly IDatabaseService _db;
        private readonly CemsApiService _cemsApi;
        private readonly ILoggingService _logger;

        public SendCemsTxResultTask(IDatabaseService db, CemsApiService cemsApi, ILoggingService logger)
        {
            _db = db;
            _cemsApi = cemsApi;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            var dt = await _db.QueryAsync<DataTable>("sp_get_tx_outbox", type: CommandType.StoredProcedure);
            if (dt.Rows.Count == 0)
                return;

            foreach (DataRow row in dt.Rows)
            {
                var json = row["PAYLOAD_JSON"]?.ToString() ?? string.Empty;
                var transaction = JsonConvertExtension.ConvertFromJson<TransactionModelV2>(json);

                var res = await _cemsApi.RegisterTransactionAsync(transaction, ct);

                var proc = res.Result && res.ECode == null
                    ? "sp_update_tx_outbox_success"
                    : "sp_update_tx_outbox_fail";

                await _db.QueryAsync<DataTable>(
                    proc,
                    new[] { DatabaseService.Param("@tx_id", MySqlDbType.VarChar, transaction.TransactionID) },
                    type: CommandType.StoredProcedure);
            }
        }
    }
}
