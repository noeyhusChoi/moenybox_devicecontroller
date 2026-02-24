using System.IO;
using System.Text.Json;
using KIOSK.Device.Abstractions;

namespace KIOSK.Admin.Services;

public static class AdminDeviceDescriptors
{
    public static async Task<IReadOnlyList<DeviceDescriptor>> LoadFromDatabaseJsonOrDefaultAsync(
        string? connectionString,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var fromDatabase = await AdminDeviceDescriptorDbLoader
            .TryLoadAsync(connectionString, cancellationToken)
            .ConfigureAwait(false);

        if (fromDatabase is { Count: > 0 })
            return fromDatabase;

        return LoadFromJsonOrDefault(filePath);
    }

    public static IReadOnlyList<DeviceDescriptor> LoadFromJsonOrDefault(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return Array.Empty<DeviceDescriptor>();

            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var items = JsonSerializer.Deserialize<List<DeviceDescriptorJson>>(json, options);
            if (items is null || items.Count == 0)
                return Array.Empty<DeviceDescriptor>();

            return items.Select(ToDescriptor).ToArray();
        }
        catch
        {
            return Array.Empty<DeviceDescriptor>();
        }
    }

    private static DeviceDescriptor ToDescriptor(DeviceDescriptorJson item)
        => new(
            Name: item.Name ?? item.DeviceId ?? string.Empty,
            Vendor: item.Vendor ?? string.Empty,
            Model: item.Model ?? string.Empty,
            TransportType: item.TransportType ?? string.Empty,
            TransportPort: item.TransportPort ?? string.Empty,
            TransportParam: item.TransportParam ?? string.Empty,
            ProtocolName: item.ProtocolName ?? string.Empty,
            PollingMs: item.PollingMs ?? 10000,
            Validate: item.Validate ?? true,
            DeviceType: item.DeviceType ?? string.Empty,
            Driver: item.Driver ?? string.Empty,
            DeviceId: item.DeviceId ?? string.Empty);

    private sealed class DeviceDescriptorJson
    {
        public string? Name { get; set; }
        public string? Vendor { get; set; }
        public string? Model { get; set; }
        public string? TransportType { get; set; }
        public string? TransportPort { get; set; }
        public string? TransportParam { get; set; }
        public string? ProtocolName { get; set; }
        public int? PollingMs { get; set; }
        public bool? Validate { get; set; }
        public string? DeviceType { get; set; }
        public string? Driver { get; set; }
        public string? DeviceId { get; set; }
    }
}
