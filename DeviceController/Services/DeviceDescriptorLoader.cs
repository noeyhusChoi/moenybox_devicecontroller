using System.IO;
using System.Text.Json;
using Kiosk.Infrastructure.Database;

namespace DeviceController.Services;

public static class DeviceDescriptorLoader
{
    public static async Task<DeviceDescriptorLoadResult> LoadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var diagnostics = new List<string>();
        foreach (var candidate in GetConnectionCandidates())
        {
            var attempt = await DeviceDescriptorDbLoader
                .TryLoadAsync(candidate.ConnectionString, candidate.SourceLabel, cancellationToken)
                .ConfigureAwait(false);

            diagnostics.Add(attempt.Diagnostic);
            if (!attempt.Succeeded)
                continue;

            return new DeviceDescriptorLoadResult(
                attempt.Descriptors,
                SourceLabel: candidate.SourceLabel,
                Summary: $"Device source: {candidate.SourceLabel} | Devices: {attempt.Descriptors.Count}",
                Diagnostics: string.Join(Environment.NewLine, diagnostics),
                LoadedFromDatabase: true);
        }

        var fallback = LoadFromJsonOrDefault(filePath);
        diagnostics.Add($"json:{Path.GetFileName(filePath)} loaded {fallback.Count} device(s)");
        return new DeviceDescriptorLoadResult(
            fallback,
            SourceLabel: $"JSON:{Path.GetFileName(filePath)}",
            Summary: $"Device source: JSON fallback | Devices: {fallback.Count}",
            Diagnostics: string.Join(Environment.NewLine, diagnostics),
            LoadedFromDatabase: false);
    }

    private static IReadOnlyList<DeviceDescriptor> LoadFromJsonOrDefault(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return Array.Empty<DeviceDescriptor>();

            var json = File.ReadAllText(filePath);
            var items = JsonSerializer.Deserialize<List<DeviceDescriptorJson>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (items is null || items.Count == 0)
                return Array.Empty<DeviceDescriptor>();

            return items
                .Select(item => new DeviceDescriptor(
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
                    DriverType: item.Driver ?? string.Empty,
                    DeviceId: item.DeviceId ?? string.Empty))
                .ToArray();
        }
        catch
        {
            return Array.Empty<DeviceDescriptor>();
        }
    }

    private static IEnumerable<(string SourceLabel, string ConnectionString)> GetConnectionCandidates()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in new[]
                 {
                     ("env:KIOSK_ADMIN_DB_CONNECTION_STRING", Environment.GetEnvironmentVariable("KIOSK_ADMIN_DB_CONNECTION_STRING")),
                     ("env:KIOSK_DB_CONNECTION_STRING", Environment.GetEnvironmentVariable("KIOSK_DB_CONNECTION_STRING")),
                     ("default:DatabaseConfig.DefaultConnectionString", DatabaseConfig.DefaultConnectionString)
                 })
        {
            if (string.IsNullOrWhiteSpace(candidate.Item2))
                continue;

            if (!seen.Add(candidate.Item2))
                continue;

            yield return (candidate.Item1, candidate.Item2);
        }
    }

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
