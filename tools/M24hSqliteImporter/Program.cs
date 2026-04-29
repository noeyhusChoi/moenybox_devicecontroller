using System.Data.Common;
using System.Globalization;
using Microsoft.Data.Sqlite;
using MySqlConnector;

var options = MigrationOptions.Parse(args);

Console.WriteLine($"MySQL source: {options.MySqlConnectionString}");
Console.WriteLine($"SQLite target: {options.SqlitePath}");

var sqliteDirectory = Path.GetDirectoryName(options.SqlitePath);
if (!string.IsNullOrWhiteSpace(sqliteDirectory))
{
    Directory.CreateDirectory(sqliteDirectory);
}

var tableMigrations = new[]
{
    new TableMigration("device_comm", ["device_id", "comm_type", "comm_port", "comm_params", "polling_ms", "created_at", "updated_at"]),
    new TableMigration("device_instance", ["device_id", "kiosk_id", "device_name", "catalog_id", "is_enabled", "created_at", "updated_at"]),
    new TableMigration("device_catalog", ["catalog_id", "vendor", "model", "driver_type", "device_type", "created_at", "updated_at"]),
    new TableMigration("device_command_log", ["id", "device_name", "command_name", "success", "error_code", "origin", "started_at", "finished_at", "duration_ms", "created_at"]),
    new TableMigration("device_status_log", ["id", "kiosk_id", "device_name", "device_type", "source", "code", "severity", "message", "created_at"]),
    new TableMigration("transaction_outbox", ["ID", "KIOSK_ID", "TRANSACTION_ID", "MESSAGE_TYPE", "PAYLOAD_JSON", "STATUS", "RETRY_COUNT", "NEXT_RETRY_AT", "LAST_TRIED_AT", "CREATED_AT"]),
    new TableMigration("cassette", ["KIOSK_ID", "DEVICE_ID", "SLOT", "CURRENCY_CODE", "DENOMINATION", "CAPACITY", "CURRENT_COUNT", "VLD", "CREATED_AT", "UPDATED_AT"]),
    new TableMigration("currency", ["KIOSK_ID", "CULTURE_CODE", "CURRENCY_CODE", "CURRENCY_DECIMAL", "CURRENCY_SYMBOL", "VLD", "CREATED_AT", "UPDATED_AT"]),
    new TableMigration("deposit_denom", ["KIOSK_ID", "CURRENCY_CODE", "VALUE", "VLD", "UPDATED_BY", "UPDATED_AT"]),
    new TableMigration("kiosk_shop", ["ID", "KIOSK_ID", "INFO_LOCALE", "INFO_KEY", "INFO_VALUE", "CREATED_AT", "UPDATED_AT"]),
    new TableMigration("kiosk_update_history", ["ID", "KIOSK_ID", "UPDATE_NO", "UPDATE_SOURCE", "UPDATE_DATETIME"]),
    new TableMigration("locale_info", ["ID", "LANGUAGE_CODE", "COUNTRY_CODE", "CULTURE_CODE", "LANGUAGE_NAME", "LANGUAGE_NAME_KO", "LANGUAGE_NAME_EN", "COUNTRY_NAME_KO", "COUNTRY_NAME_EN"]),
    new TableMigration("server", ["ID", "KIOSK_ID", "SERVER_NAME", "SERVER_URL", "SERVER_KEY", "TIMEOUT_SECONDS", "VLD", "CREATED_AT", "UPDATED_AT"]),
    new TableMigration("deposit_denom_attribute", ["ID", "KIOSK_ID", "CURRENCY_CODE", "VALUE", "ATTRIBUTE_CODE", "VLD", "CREATED_AT", "UPDATED_AT"]),
    new TableMigration("kiosk", ["KIOSK_ID", "KIOSK_PID", "VLD", "CREATED_AT", "UPDATED_AT"]),
};

await using var mySqlConnection = new MySqlConnection(options.MySqlConnectionString);
await mySqlConnection.OpenAsync();

await using var sqliteConnection = new SqliteConnection($"Data Source={options.SqlitePath}");
await sqliteConnection.OpenAsync();

await using (var pragmaCommand = sqliteConnection.CreateCommand())
{
    pragmaCommand.CommandText = "PRAGMA foreign_keys = OFF;";
    await pragmaCommand.ExecuteNonQueryAsync();
}

await using var sqliteTransaction = await sqliteConnection.BeginTransactionAsync();

try
{
    foreach (var migration in tableMigrations)
    {
        await ClearDestinationTableAsync(sqliteConnection, sqliteTransaction, migration.TableName);
    }

    Array.Reverse(tableMigrations);

    foreach (var migration in tableMigrations)
    {
        var inserted = await CopyTableAsync(mySqlConnection, sqliteConnection, sqliteTransaction, migration);
        Console.WriteLine($"{migration.TableName}: {inserted} rows copied");
    }

    await sqliteTransaction.CommitAsync();
}
catch
{
    await sqliteTransaction.RollbackAsync();
    throw;
}
finally
{
    await using var pragmaCommand = sqliteConnection.CreateCommand();
    pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
    await pragmaCommand.ExecuteNonQueryAsync();
}

Console.WriteLine();
Console.WriteLine("Verification counts:");

foreach (var migration in tableMigrations)
{
    var count = await CountRowsAsync(sqliteConnection, migration.TableName);
    Console.WriteLine($"{migration.TableName}: {count}");
}

return;

static async Task ClearDestinationTableAsync(SqliteConnection connection, DbTransaction transaction, string tableName)
{
    await using var deleteCommand = connection.CreateCommand();
    deleteCommand.Transaction = (SqliteTransaction)transaction;
    deleteCommand.CommandText = $"DELETE FROM {tableName};";
    await deleteCommand.ExecuteNonQueryAsync();
}

static async Task<long> CountRowsAsync(SqliteConnection connection, string tableName)
{
    await using var countCommand = connection.CreateCommand();
    countCommand.CommandText = $"SELECT COUNT(*) FROM {tableName};";
    return Convert.ToInt64(await countCommand.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
}

static async Task<int> CopyTableAsync(
    MySqlConnection mySqlConnection,
    SqliteConnection sqliteConnection,
    DbTransaction sqliteTransaction,
    TableMigration migration)
{
    var columnList = string.Join(", ", migration.Columns);

    await using var sourceCommand = mySqlConnection.CreateCommand();
    sourceCommand.CommandText = $"SELECT {columnList} FROM {migration.TableName};";

    await using var reader = await sourceCommand.ExecuteReaderAsync();
    await using var insertCommand = sqliteConnection.CreateCommand();
    insertCommand.Transaction = (SqliteTransaction)sqliteTransaction;
    insertCommand.CommandText = $"INSERT INTO {migration.TableName} ({columnList}) VALUES ({string.Join(", ", migration.Columns.Select((_, index) => $"$p{index}"))});";

    for (var i = 0; i < migration.Columns.Length; i++)
    {
        insertCommand.Parameters.Add(new SqliteParameter($"$p{i}", DBNull.Value));
    }

    var count = 0;

    while (await reader.ReadAsync())
    {
        for (var i = 0; i < migration.Columns.Length; i++)
        {
            var rawValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
            insertCommand.Parameters[i].Value = NormalizeValue(migration.TableName, migration.Columns[i], rawValue);
        }

        await insertCommand.ExecuteNonQueryAsync();
        count++;
    }

    return count;
}

static object NormalizeValue(string tableName, string columnName, object? value)
{
    if (value is null)
    {
        return DBNull.Value;
    }

    if (tableName.Equals("transaction_outbox", StringComparison.OrdinalIgnoreCase)
        && columnName.Equals("TRANSACTION_ID", StringComparison.OrdinalIgnoreCase))
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    return value switch
    {
        DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
        TimeOnly timeOnly => timeOnly.ToTimeSpan(),
        _ => value
    };
}

sealed record TableMigration(string TableName, string[] Columns);

sealed class MigrationOptions
{
    private const string DefaultMySqlConnectionString =
        "Server=localhost;Port=3307;Database=m24h;User ID=dev;Password=devP@ss!;AllowUserVariables=True;ConnectionReset=false;DefaultCommandTimeout=300;SslMode=Required;";

    public required string MySqlConnectionString { get; init; }
    public required string SqlitePath { get; init; }

    public static MigrationOptions Parse(string[] args)
    {
        var mysqlConnectionString = DefaultMySqlConnectionString;
        string? sqlitePath = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--mysql-connection" when i + 1 < args.Length:
                    mysqlConnectionString = args[++i];
                    break;
                case "--sqlite-path" when i + 1 < args.Length:
                    sqlitePath = args[++i];
                    break;
            }
        }

        return new MigrationOptions
        {
            MySqlConnectionString = mysqlConnectionString,
            SqlitePath = sqlitePath ?? ResolveDefaultSqlitePath(),
        };
    }

    private static string ResolveDefaultSqlitePath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("KIOSK_SQLITE_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "MoneyBox", "Kiosk", "m24h.db");
    }
}
