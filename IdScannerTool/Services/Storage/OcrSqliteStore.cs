using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;

namespace IdScannerTool.Services;

public sealed class OcrSqliteStore : IOcrHistoryStore
{
    private const string TableName = "ocr_history";
    private const string UsageSyncPending = "PENDING";
    private const string UsageSyncSucceeded = "SUCCEEDED";
    private const string UsageSyncFailed = "FAILED";

    private const string SelectRowsSql =
        $"""
         SELECT id, timestamp_utc, document_type, document_no, name, nationality, birth_date, expiry_date, raw_json
         FROM {TableName}
         ORDER BY id DESC;
         """;

    private readonly string _dbPath;
    private readonly object _sync = new();
    private bool _initialized;

    public OcrSqliteStore(string? dbPath = null)
    {
        _dbPath = string.IsNullOrWhiteSpace(dbPath)
            ? Path.Combine(AppContext.BaseDirectory, "ocr-history.db")
            : dbPath;
    }

    public Task<IReadOnlyList<OcrHistoryRow>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.Run<IReadOnlyList<OcrHistoryRow>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_sync)
            {
                EnsureInitialized();
                using var connection = OpenConnection();
                return QueryRows(connection, cancellationToken);
            }
        }, cancellationToken);

    public Task<long> AddAsync(
        IReadOnlyDictionary<string, string> fields,
        string? documentType,
        string? deviceSerial,
        string rawJson,
        CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_sync)
            {
                var timestamp = DateTimeOffset.UtcNow;
                EnsureInitialized();
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"""
                    INSERT INTO {TableName}
                    (timestamp_utc, document_type, document_no, name, nationality, birth_date, expiry_date, raw_json, device_serial, usage_sync_status, usage_sync_retry_count, usage_sync_last_error, usage_sync_last_attempt_utc, usage_sync_date_key, usage_sync_total_usage, usage_sync_last_response)
                    VALUES
                    ($timestamp_utc, $document_type, $document_no, $name, $nationality, $birth_date, $expiry_date, $raw_json, $device_serial, $usage_sync_status, $usage_sync_retry_count, $usage_sync_last_error, $usage_sync_last_attempt_utc, $usage_sync_date_key, $usage_sync_total_usage, $usage_sync_last_response);
                    SELECT last_insert_rowid();
                    """;
                command.Parameters.AddWithValue("$timestamp_utc", timestamp.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$document_type", documentType ?? string.Empty);
                command.Parameters.AddWithValue("$document_no", TryGet(fields, "NO"));
                command.Parameters.AddWithValue("$name", TryGet(fields, "NAME"));
                command.Parameters.AddWithValue("$nationality", TryGet(fields, "NATIONALITY"));
                command.Parameters.AddWithValue("$birth_date", TryGet(fields, "BIRTHDATE"));
                command.Parameters.AddWithValue("$expiry_date", TryGet(fields, "EXPIRYDATE"));
                command.Parameters.AddWithValue("$raw_json", rawJson ?? string.Empty);
                command.Parameters.AddWithValue("$device_serial", deviceSerial ?? string.Empty);
                command.Parameters.AddWithValue("$usage_sync_status", UsageSyncPending);
                command.Parameters.AddWithValue("$usage_sync_retry_count", 0);
                command.Parameters.AddWithValue("$usage_sync_last_error", string.Empty);
                command.Parameters.AddWithValue("$usage_sync_last_attempt_utc", string.Empty);
                command.Parameters.AddWithValue("$usage_sync_date_key", string.Empty);
                command.Parameters.AddWithValue("$usage_sync_total_usage", DBNull.Value);
                command.Parameters.AddWithValue("$usage_sync_last_response", string.Empty);
                return (long)(command.ExecuteScalar() ?? 0L);
            }
        }, cancellationToken);

    public Task<int> DeleteByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ids.Count == 0)
            {
                return 0;
            }

            lock (_sync)
            {
                EnsureInitialized();
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();

                var parameterNames = new List<string>(ids.Count);
                var index = 0;
                foreach (var id in ids.Distinct())
                {
                    var paramName = $"$id{index++}";
                    parameterNames.Add(paramName);
                    command.Parameters.AddWithValue(paramName, id);
                }

                command.CommandText = $"DELETE FROM {TableName} WHERE id IN ({string.Join(", ", parameterNames)});";
                return command.ExecuteNonQuery();
            }
        }, cancellationToken);

    public Task<IReadOnlyList<OcrHistoryUsageSyncRow>> GetPendingUsageSyncRowsAsync(
        int maxCount = 100,
        CancellationToken cancellationToken = default)
        => Task.Run<IReadOnlyList<OcrHistoryUsageSyncRow>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_sync)
            {
                EnsureInitialized();
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"""
                    SELECT id, device_serial, usage_sync_status, usage_sync_retry_count, usage_sync_last_error, usage_sync_last_attempt_utc, usage_sync_last_response
                    FROM {TableName}
                    WHERE usage_sync_status IN ($pending, $failed)
                    ORDER BY id ASC
                    LIMIT $maxCount;
                    """;
                command.Parameters.AddWithValue("$pending", UsageSyncPending);
                command.Parameters.AddWithValue("$failed", UsageSyncFailed);
                command.Parameters.AddWithValue("$maxCount", maxCount);

                using var reader = command.ExecuteReader();
                var rows = new List<OcrHistoryUsageSyncRow>();
                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    rows.Add(new OcrHistoryUsageSyncRow(
                        reader.GetInt64(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetInt32(3),
                        reader.GetString(4),
                        ParseNullableTimestamp(reader.GetString(5)),
                        reader.GetString(6)));
                }

                return rows;
            }
        }, cancellationToken);

    public Task MarkUsageSyncSucceededAsync(
        long id,
        string? dateKey,
        int? totalUsage,
        string? responseBody,
        CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_sync)
            {
                EnsureInitialized();
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"""
                    UPDATE {TableName}
                    SET usage_sync_status = $status,
                        usage_sync_last_error = $last_error,
                        usage_sync_last_attempt_utc = $last_attempt_utc,
                        usage_sync_date_key = $date_key,
                        usage_sync_total_usage = $total_usage,
                        usage_sync_last_response = $last_response
                    WHERE id = $id;
                    """;
                command.Parameters.AddWithValue("$status", UsageSyncSucceeded);
                command.Parameters.AddWithValue("$last_error", string.Empty);
                command.Parameters.AddWithValue("$last_attempt_utc", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$date_key", dateKey ?? string.Empty);
                command.Parameters.AddWithValue("$total_usage", totalUsage.HasValue ? totalUsage.Value : DBNull.Value);
                command.Parameters.AddWithValue("$last_response", responseBody ?? string.Empty);
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }, cancellationToken);

    public Task MarkUsageSyncFailedAsync(
        long id,
        string error,
        string? responseBody,
        CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_sync)
            {
                EnsureInitialized();
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"""
                    UPDATE {TableName}
                    SET usage_sync_status = $status,
                        usage_sync_retry_count = usage_sync_retry_count + 1,
                        usage_sync_last_error = $last_error,
                        usage_sync_last_attempt_utc = $last_attempt_utc,
                        usage_sync_last_response = $last_response
                    WHERE id = $id;
                    """;
                command.Parameters.AddWithValue("$status", UsageSyncFailed);
                command.Parameters.AddWithValue("$last_error", error ?? string.Empty);
                command.Parameters.AddWithValue("$last_attempt_utc", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$last_response", responseBody ?? string.Empty);
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }, cancellationToken);

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            CREATE TABLE IF NOT EXISTS {TableName}
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp_utc TEXT NOT NULL,
                document_type TEXT NOT NULL DEFAULT '',
                document_no TEXT NOT NULL DEFAULT '',
                name TEXT NOT NULL DEFAULT '',
                nationality TEXT NOT NULL DEFAULT '',
                sex TEXT NOT NULL DEFAULT '',
                birth_date TEXT NOT NULL DEFAULT '',
                expiry_date TEXT NOT NULL DEFAULT '',
                raw_json TEXT NOT NULL DEFAULT '',
                device_serial TEXT NOT NULL DEFAULT '',
                usage_sync_status TEXT NOT NULL DEFAULT 'PENDING',
                usage_sync_retry_count INTEGER NOT NULL DEFAULT 0,
                usage_sync_last_error TEXT NOT NULL DEFAULT '',
                usage_sync_last_attempt_utc TEXT NOT NULL DEFAULT '',
                usage_sync_date_key TEXT NOT NULL DEFAULT '',
                usage_sync_total_usage INTEGER NULL,
                usage_sync_last_response TEXT NOT NULL DEFAULT ''
            );
            """;
        command.ExecuteNonQuery();

        EnsureColumnExists(connection, "document_type", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "device_serial", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "usage_sync_status", $"TEXT NOT NULL DEFAULT '{UsageSyncPending}'");
        EnsureColumnExists(connection, "usage_sync_retry_count", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumnExists(connection, "usage_sync_last_error", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "usage_sync_last_attempt_utc", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "usage_sync_date_key", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "usage_sync_total_usage", "INTEGER NULL");
        EnsureColumnExists(connection, "usage_sync_last_response", "TEXT NOT NULL DEFAULT ''");
        _initialized = true;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_dbPath};Mode=ReadWriteCreate;Cache=Shared");
        connection.Open();
        return connection;
    }

    private static List<OcrHistoryRow> QueryRows(SqliteConnection connection, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = SelectRowsSql;

        using var reader = command.ExecuteReader();
        var rows = new List<OcrHistoryRow>();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(new OcrHistoryRow(
                reader.GetInt64(0),
                ParseTimestamp(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8)));
        }

        return rows;
    }

    private static DateTimeOffset ParseTimestamp(string value)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.MinValue;
    }

    private static DateTimeOffset? ParseNullableTimestamp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string TryGet(IReadOnlyDictionary<string, string> fields, string key)
        => fields.TryGetValue(key, out var value) ? value : string.Empty;

    private static void EnsureColumnExists(SqliteConnection connection, string columnName, string columnDefinition)
    {
        using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({TableName});";
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {TableName} ADD COLUMN {columnName} {columnDefinition};";
        alter.ExecuteNonQuery();
    }
}

public sealed record OcrHistoryRow(
    long Id,
    DateTimeOffset TimestampUtc,
    string DocumentType,
    string DocumentNo,
    string Name,
    string Nationality,
    string BirthDate,
    string ExpiryDate,
    string RawJson);

public sealed record OcrHistoryUsageSyncRow(
    long Id,
    string DeviceSerial,
    string UsageSyncStatus,
    int UsageSyncRetryCount,
    string UsageSyncLastError,
    DateTimeOffset? UsageSyncLastAttemptUtc,
    string UsageSyncLastResponse);
