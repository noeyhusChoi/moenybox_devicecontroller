using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;

namespace IdScannerTool.Services;

public sealed class OcrSqliteStore : IOcrHistoryStore
{
    private const string TableName = "ocr_history";
    private const string SelectRowsSql =
        $"""
         SELECT id, timestamp_utc, document_type, document_no, name, nationality, sex, birth_date, expiry_date, raw_json
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

    public Task AddAsync(
        IReadOnlyDictionary<string, string> fields,
        string? documentType,
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
                    (timestamp_utc, document_type, document_no, name, nationality, sex, birth_date, expiry_date, raw_json)
                    VALUES
                    ($timestamp_utc, $document_type, $document_no, $name, $nationality, $sex, $birth_date, $expiry_date, $raw_json);
                    """;
                command.Parameters.AddWithValue("$timestamp_utc", timestamp.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$document_type", documentType ?? string.Empty);
                command.Parameters.AddWithValue("$document_no", TryGet(fields, "NO"));
                command.Parameters.AddWithValue("$name", TryGet(fields, "NAME"));
                command.Parameters.AddWithValue("$nationality", TryGet(fields, "NATIONALITY"));
                command.Parameters.AddWithValue("$sex", TryGet(fields, "SEX"));
                command.Parameters.AddWithValue("$birth_date", TryGet(fields, "BIRTHDATE"));
                command.Parameters.AddWithValue("$expiry_date", TryGet(fields, "EXPIRYDATE"));
                command.Parameters.AddWithValue("$raw_json", rawJson ?? string.Empty);
                command.ExecuteNonQuery();
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
                raw_json TEXT NOT NULL DEFAULT ''
            );
            """;
        command.ExecuteNonQuery();
        EnsureColumnExists(connection, "document_type", "TEXT NOT NULL DEFAULT ''");
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
                reader.GetString(8),
                reader.GetString(9)));
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
    string Sex,
    string BirthDate,
    string ExpiryDate,
    string RawJson);
