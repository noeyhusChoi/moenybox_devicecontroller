using KIOSK.Infrastructure.Database;
using MySqlConnector;
using System.Data;

namespace KIOSK.Application.Services.Transactions
{
    public sealed class TransactionOutboxService : ITransactionOutboxService
    {
        private readonly IDatabaseService _db;

        public TransactionOutboxService(IDatabaseService db)
        {
            _db = db;
        }

        public Task MarkSuccessAsync(string transactionId, CancellationToken ct = default)
            => _db.QueryAsync<DataTable>(
                "sp_update_tx_outbox_success",
                new[] { DatabaseService.Param("@tx_id", MySqlDbType.VarChar, transactionId) },
                type: CommandType.StoredProcedure,
                ct: ct);

        public Task MarkFailAsync(string transactionId, CancellationToken ct = default)
            => _db.QueryAsync<DataTable>(
                "sp_update_tx_outbox_fail",
                new[] { DatabaseService.Param("@tx_id", MySqlDbType.VarChar, transactionId) },
                type: CommandType.StoredProcedure,
                ct: ct);
    }
}
