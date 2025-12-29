using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Database.Interface;
using MySqlConnector;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public sealed class DeviceCommandLogRepository
        : RepositoryBase,
          ICreateRepository<DeviceCommandRecord>
    {
        private const string InsertProc = "sp_insert_device_command_log";

        public DeviceCommandLogRepository(IDatabaseService db) : base(db)
        {
        }

        public Task SaveAsync(DeviceCommandRecord record)
            => InsertAsync(record);

        public Task InsertAsync(DeviceCommandRecord record, CancellationToken ct = default)
        {
            var parameters = new[]
            {
                P("@p_device_name", record.Name, MySqlDbType.VarChar),
                P("@p_command", record.Command, MySqlDbType.VarChar),
                P("@p_success", record.Success ? 1 : 0, MySqlDbType.Int32),
                P("@p_error_code", record.ErrorCode?.ToString(), MySqlDbType.VarChar),
                P("@p_origin", record.Origin.ToString(), MySqlDbType.VarChar),
                P("@p_started_at", record.StartedAt.UtcDateTime, MySqlDbType.DateTime),
                P("@p_finished_at", record.FinishedAt.UtcDateTime, MySqlDbType.DateTime),
                P("@p_duration_ms", record.DurationMs, MySqlDbType.Int64),
            };

            return ExecAsync(InsertProc, parameters, ct);
        }
    }
}
