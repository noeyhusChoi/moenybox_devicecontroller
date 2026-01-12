using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Cache;
using KIOSK.Infrastructure.Database.Interface;
using MySqlConnector;

namespace KIOSK.Infrastructure.Database.Repositories
{
    public sealed class DeviceStatusLogRepository : RepositoryBase
    {
        private const string InsertProc = "sp_insert_device_status_log";
        private readonly DatabaseCache _cache;

        public DeviceStatusLogRepository(IDatabaseService db, DatabaseCache cache) : base(db)
        {
            _cache = cache;
        }

        public Task SaveAsync(string name, StatusSnapshot snapshot, CancellationToken ct = default)
        {
            if (snapshot.Alerts is null || snapshot.Alerts.Count == 0)
                return Task.CompletedTask;

            var kioskId = _cache.Kiosk.FirstOrDefault()?.Id;
            if (string.IsNullOrWhiteSpace(kioskId))
                return Task.CompletedTask;

            var deviceType = _cache.DeviceList
                .FirstOrDefault(d =>
                    string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.Id, name, StringComparison.OrdinalIgnoreCase))
                ?.DeviceType;
            if (string.IsNullOrWhiteSpace(deviceType))
                return Task.CompletedTask;

            var tasks = new List<Task>(snapshot.Alerts.Count);
            foreach (var alert in snapshot.Alerts)
                tasks.Add(InsertAsync(kioskId, name, deviceType, alert, ct));

            return Task.WhenAll(tasks);
        }

        private Task InsertAsync(string kioskId, string name, string deviceType, StatusEvent alert, CancellationToken ct)
        {
            var parameters = new[]
            {
                P("@p_kiosk_id", kioskId, MySqlDbType.VarChar),
                P("@p_device_name", name, MySqlDbType.VarChar),
                P("@p_device_type", deviceType, MySqlDbType.VarChar),
                P("@p_source", alert.Source.ToString(), MySqlDbType.VarChar),
                P("@p_code", alert.ErrorCode?.ToString() ?? alert.Code, MySqlDbType.VarChar),
                P("@p_severity", alert.Severity.ToString(), MySqlDbType.VarChar),
                P("@p_message", alert.Message, MySqlDbType.VarChar),
                P("@p_created_at", alert.At.UtcDateTime, MySqlDbType.DateTime)
            };

            return ExecAsync(InsertProc, parameters, ct);
        }
    }
}
