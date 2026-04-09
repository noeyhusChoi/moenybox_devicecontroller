using System.IO;
using System.Text.Json;

namespace DeviceController.Services;

public static class RuntimeOptionsLoader
{
    public static DeviceRuntimeOptions LoadOrDefault(string filePath)
    {
        var options = DeviceRuntimeOptions.Default;

        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var parsed = JsonSerializer.Deserialize<RuntimeOptionsJson>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed is not null)
                {
                    options = options with
                    {
                        DefaultPollingMs = parsed.DefaultPollingMs ?? options.DefaultPollingMs,
                        MinPollingMs = parsed.MinPollingMs ?? options.MinPollingMs,
                        MaxBackoffMs = parsed.MaxBackoffMs ?? options.MaxBackoffMs,
                        SchedulerTickMs = parsed.SchedulerTickMs ?? options.SchedulerTickMs
                    };
                }
            }
        }
        catch
        {
        }

        return options.Normalize();
    }

    private sealed class RuntimeOptionsJson
    {
        public int? DefaultPollingMs { get; set; }
        public int? MinPollingMs { get; set; }
        public int? MaxBackoffMs { get; set; }
        public int? SchedulerTickMs { get; set; }
    }
}
