using KIOSK.Services;
using MySqlConnector;
using KIOSK.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Database
{
    public interface IDatabaseService
    {
        Task<bool> CanConnectAsync(CancellationToken ct = default);

        Task<int> ExecuteAsync(
            string sqlOrProc,
            MySqlParameter[]? parameters = null,
            CommandType type = CommandType.Text,
            CancellationToken ct = default);

        Task<T> QueryAsync<T>(
            string sqlOrProc,
            MySqlParameter[]? parameters = null,
            CommandType type = CommandType.Text,
            CancellationToken ct = default)
            where T : class, new();

        Task WithTransactionAsync(
            Func<MySqlConnection, MySqlTransaction, Task> work,
            IsolationLevel iso = IsolationLevel.ReadCommitted,
            CancellationToken ct = default);

        // 편의 파라미터 빌더
        public static MySqlParameter Param(string name, MySqlDbType type, object? value, int? size = null)
            => throw new NotImplementedException();
    }

    [Serializable]
    public sealed class DatabaseService : IDatabaseService, IDisposable
    {
        private readonly ILoggingService _logging;
        private readonly string _connectionString;
        private readonly int _timeoutSec;
        private bool _disposed;

        public DatabaseService(ILoggingService logging, string? connectionString = null, int commandTimeoutSeconds = 300)
        {
            _logging = logging;
            
            // TODO: ConnectionString 보안 처리 후 가져오기
            _connectionString =
                connectionString ??
                "Server=4.218.15.147;Port=3306;Database=m24h;User ID=dev;Password=devP@ss!;AllowUserVariables=True;ConnectionReset=false;DefaultCommandTimeout=300;SslMode=Required;";
            _timeoutSec = commandTimeoutSeconds;
        }

        // 연결
        private MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);

        // 연결 가능 상태 확인
        public async Task<bool> CanConnectAsync(CancellationToken ct = default)
        {
            try
            {
                await using var connection = CreateConnection();
                await connection.OpenAsync(ct).ConfigureAwait(false);
                return await connection.PingAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                _logging.Error(ex, "DB Connection Failed");
                //Console.Error.WriteLine($"[CanConnectAsync] DB Connection Failed: {ex}");
                return false;
            }
        }

        // 변경
        public async Task<int> ExecuteAsync(string sqlOrProc,
                                            MySqlParameter[]? parameters = null,
                                            CommandType type = CommandType.Text,
                                            CancellationToken ct = default)
        {
            try
            {
                await using var connection = CreateConnection();
                await connection.OpenAsync(ct).ConfigureAwait(false);
                await using var cmd = new MySqlCommand(sqlOrProc, connection)
                {
                    CommandType = type,
                    CommandTimeout = _timeoutSec
                };
                if (parameters is { Length: > 0 }) cmd.Parameters.AddRange(parameters);

                return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            catch (MySqlException ex)
            {
                _logging.Error(ex, $"MySQL Error (Number: {ex.Number})");
                //Console.Error.WriteLine($"[ExecuteAsync] MySQL Error: {ex.Message} (Number: {ex.Number})");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logging.Error(ex, "DB Connection Failed");
                //Console.Error.WriteLine($"[ExecuteAsync] Timeout Error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logging.Error(ex, "DB Exception");
                //Console.Error.WriteLine($"[ExecuteAsync] Exception: {ex}");
                throw;
            }
        }

        // 조회 (DataTable or DataSet)
        public async Task<T> QueryAsync<T>(string sqlOrProc,
                                           MySqlParameter[]? parameters = null,
                                           CommandType type = CommandType.Text,
                                           CancellationToken ct = default)
                                           where T : class, new()
        {
            try
            {
                await using var connection = CreateConnection();
                await connection.OpenAsync(ct).ConfigureAwait(false);
                await using var cmd = new MySqlCommand(sqlOrProc, connection)
                {
                    CommandType = type,
                    CommandTimeout = _timeoutSec
                };
                if (parameters is { Length: > 0 }) cmd.Parameters.AddRange(parameters);

                using var adapter = new MySqlDataAdapter(cmd);

                if (typeof(T) == typeof(DataTable))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt as T ?? throw new InvalidCastException();
                }
                else if (typeof(T) == typeof(DataSet))
                {
                    var ds = new DataSet();
                    adapter.Fill(ds);
                    return ds as T ?? throw new InvalidCastException();
                }

                throw new NotSupportedException(
                    $"QueryAsync<T> is only support DataSet, DataTable. Not Support Type: {typeof(T).FullName}");
            }
            catch (MySqlException ex)
            {
                _logging.Error(ex, $"MySQL Error (Number: {ex.Number})");
                //Console.Error.WriteLine($"[ExecuteAsync] MySQL Error: {ex.Message} (Number: {ex.Number})");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logging.Error(ex, "DB Connection Failed");
                //Console.Error.WriteLine($"[ExecuteAsync] Timeout Error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logging.Error(ex, "DB Exception");
                //Console.Error.WriteLine($"[ExecuteAsync] Exception: {ex}");
                throw;
            }
        }

        // 트랜잭션
        public async Task WithTransactionAsync(Func<MySqlConnection, MySqlTransaction, Task> work,
                                               IsolationLevel isolation = IsolationLevel.ReadCommitted,
                                               CancellationToken ct = default)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(ct).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(isolation, ct).ConfigureAwait(false);
            try
            {
                await work(connection, transaction).ConfigureAwait(false);
                await transaction.CommitAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(ct).ConfigureAwait(false);
                }
                catch (Exception rbEx)
                {
                    _logging.Error(ex, "Transaction RollBack Fail");
                    //Console.Error.WriteLine($"[WithTransactionAsync] RollBack Fail: {rbEx}");
                }

                _logging.Error(ex, "Transaction Error");
                //Console.Error.WriteLine($"[WithTransactionAsync] Transaction Error: {ex}");
                throw;
            }
        }

        // IDisposable
        public void Dispose()
        {
            if (_disposed) return;
            // 필요 시 내부 자원 정리 (ex: logger, pooled objects)
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        // 파라미터
        public static MySqlParameter Param(string name, MySqlDbType type, object? value, int? size = null)
        {
            var param = new MySqlParameter(name, type);
            if (size.HasValue) param.Size = size.Value;
            param.Value = value ?? DBNull.Value;
            return param;
        }
    }
}

/* 사용 예시
    // 조회 (제네릭, DataSet, DataTable)
    var res = await db.QueryAsync<DataSet>(
        "SELECT shop_name, shop_tel FROM kiosk_shop WHERE kiosk_id=@k AND pid=@p LIMIT 1",
        new[]
        {
            DataBaseService.P("@k", MySqlDbType.VarChar, "C4E7I4W5C4B6L3K4T2C4"),
            DataBaseService.P("@p", MySqlDbType.VarChar, "1", size:16)
        });
    
    // 변경
    int rows = await db.ExecuteAsync(
        "UPDATE kiosk_shop SET shop_name=@n, shop_tel=@t WHERE kiosk_id=@k AND pid=@p",
        new[]
        {
            DataBaseService.Param("@n", MySqlDbType.VarChar, "본점(수정)"),
            DataBaseService.Param("@t", MySqlDbType.VarChar, "051-742-9998"),
            DataBaseService.Param("@k", MySqlDbType.VarChar, "C4E7I4W5C4B6L3K4T2C4"),
            DataBaseService.Param("@p", MySqlDbType.VarChar, "1")
        });
    
    // 프로시저 (제네릭, DataSet, DataTable)
    var res = await db.QueryAsync<DataSet>(
        "sp_get_recent_update_history",
        new[]
        {
            DataBaseService.Param("@p_kiosk_id", MySqlDbType.VarChar, "C4E7..."),
            DataBaseService.Param("@p_pid", MySqlDbType.VarChar, "1"),
            DataBaseService.Param("@p_limit", MySqlDbType.Int32, 20)
        },
        CommandType.StoredProcedure);
    
    // 트랜잭션
    await db.WithTransactionAsync(async (cn, tx) =>
    {
        using var c1 = new MySqlCommand("INSERT INTO admin_history(kiosk_id,pid,action,memo) VALUES(@k,@p,@a,@m)", cn, tx);
        c1.Parameters.AddRange(new[]
        {
            DataBaseService.Param("@k", MySqlDbType.VarChar, "C4E7..."),
            DataBaseService.Param("@p", MySqlDbType.VarChar, "1"),
            DataBaseService.Param("@a", MySqlDbType.VarChar, "UPDATE_SHOP"),
            DataBaseService.Param("@m", MySqlDbType.VarChar, "changed name/tel")
        });
        await c1.ExecuteNonQueryAsync();
    
        using var c2 = new MySqlCommand("UPDATE kiosk_shop SET shop_name=@n WHERE kiosk_id=@k AND pid=@p", cn, tx);
        c2.Parameters.AddRange(new[]
        {
            DataBaseService.Param("@n", MySqlDbType.VarChar, "본점(수정)"),
            DataBaseService.Param("@k", MySqlDbType.VarChar, "C4E7..."),
            DataBaseService.Param("@p", MySqlDbType.VarChar, "1")
        });
        await c2.ExecuteNonQueryAsync();
    }); 
 */