using System;
using System.IO;

namespace Kiosk.Infrastructure.Database;

public static class DatabaseConfig
{
    public static string DefaultDatabasePath => ResolveDatabasePath();

    public static string DefaultConnectionString => $"Data Source={DefaultDatabasePath}";

    private static string ResolveDatabasePath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("KIOSK_SQLITE_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var configuredDirectory = Path.GetDirectoryName(configuredPath);
            if (!string.IsNullOrWhiteSpace(configuredDirectory))
                Directory.CreateDirectory(configuredDirectory);

            return configuredPath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectory = Path.Combine(localAppData, "MoneyBox", "Kiosk");
        Directory.CreateDirectory(dataDirectory);

        return Path.Combine(dataDirectory, "m24h.db");
    }
}
