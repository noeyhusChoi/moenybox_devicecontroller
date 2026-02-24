using KIOSK.Device.Abstractions;
using MySqlConnector;

namespace KIOSK.Admin.Services;

internal static class AdminDeviceDescriptorDbLoader
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

    public static async Task<IReadOnlyList<DeviceDescriptor>?> TryLoadAsync(
        string? connectionString,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

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

                var pollingMs = GetInt32(reader, "polling_ms");
                descriptors.Add(new DeviceDescriptor(
                    // KIOSK AppInitializer + DeviceRepository 매핑과 동일하게 유지
                    Name: GetString(reader, "device_name"),
                    Vendor: GetString(reader, "vendor"),
                    Model: GetString(reader, "model"),
                    TransportType: GetString(reader, "comm_type"),
                    TransportPort: GetString(reader, "comm_port"),
                    TransportParam: GetString(reader, "comm_params"),
                    ProtocolName: string.Empty,
                    PollingMs: pollingMs,
                    Validate: true,
                    DeviceType: GetString(reader, "device_type"),
                    Driver: GetString(reader, "driver_type"),
                    DeviceId: deviceId));
            }

            return descriptors;
        }
        catch
        {
            return null;
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
}
