using MySqlConnector;

namespace DeviceController.Services;

internal static class DeviceDescriptorDbLoader
{
    private const string Query = """
        SELECT
            i.device_id,
            i.device_name,
            c.vendor,
            c.model,
            c.driver_type,
            c.device_type,
            cm.comm_type,
            cm.comm_port,
            cm.comm_params,
            cm.polling_ms
        FROM device_instance i
        LEFT JOIN device_catalog c ON c.catalog_id = i.catalog_id
        LEFT JOIN device_comm cm ON cm.device_id = i.device_id
        WHERE i.is_enabled = 1
        ORDER BY i.device_name;
        """;

    public static async Task<DeviceDescriptorDbLoadAttempt> TryLoadAsync(
        string? connectionString,
        string sourceLabel,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new DeviceDescriptorDbLoadAttempt(
                Succeeded: false,
                Descriptors: Array.Empty<DeviceDescriptor>(),
                Diagnostic: $"{sourceLabel}: skipped (empty connection string)");
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = Query;

            var descriptors = new List<DeviceDescriptor>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var deviceId = GetString(reader, "device_id");
                if (string.IsNullOrWhiteSpace(deviceId))
                    continue;

                descriptors.Add(new DeviceDescriptor(
                    Name: GetString(reader, "device_name"),
                    Vendor: GetString(reader, "vendor"),
                    Model: GetString(reader, "model"),
                    TransportType: GetString(reader, "comm_type"),
                    TransportPort: GetString(reader, "comm_port"),
                    TransportParam: GetString(reader, "comm_params"),
                    ProtocolName: string.Empty,
                    PollingMs: GetInt32(reader, "polling_ms"),
                    Validate: true,
                    DeviceType: GetString(reader, "device_type"),
                    DriverType: GetString(reader, "driver_type"),
                    DeviceId: deviceId));
            }

            return new DeviceDescriptorDbLoadAttempt(
                Succeeded: true,
                Descriptors: descriptors,
                Diagnostic: $"{sourceLabel}: database loaded {descriptors.Count} device(s)");
        }
        catch (Exception ex)
        {
            return new DeviceDescriptorDbLoadAttempt(
                Succeeded: false,
                Descriptors: Array.Empty<DeviceDescriptor>(),
                Diagnostic: $"{sourceLabel}: database load failed - {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string GetString(MySqlDataReader reader, string column, string fallback = "")
    {
        var value = reader[column];
        if (value is null || value is DBNull)
            return fallback;

        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static int GetInt32(MySqlDataReader reader, string column)
    {
        var value = reader[column];
        if (value is null || value is DBNull)
            return 0;

        return Convert.ToInt32(value);
    }

    internal sealed record DeviceDescriptorDbLoadAttempt(
        bool Succeeded,
        IReadOnlyList<DeviceDescriptor> Descriptors,
        string Diagnostic);
}
