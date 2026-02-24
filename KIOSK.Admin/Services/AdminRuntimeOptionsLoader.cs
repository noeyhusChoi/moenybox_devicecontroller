using System.IO;
using System.Text.Json;
using KIOSK.DeviceRuntime.Ports;

namespace KIOSK.Admin.Services;

public static class AdminRuntimeOptionsLoader
{
    public static ScheduledDeviceRuntimeOptions LoadOrDefault(string filePath)
    {
        var options = ScheduledDeviceRuntimeOptions.Default;

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

        options = options with
        {
            DefaultPollingMs = ReadIntEnv("KIOSK_ADMIN_DEFAULT_POLLING_MS", options.DefaultPollingMs),
            MinPollingMs = ReadIntEnv("KIOSK_ADMIN_MIN_POLLING_MS", options.MinPollingMs),
            MaxBackoffMs = ReadIntEnv("KIOSK_ADMIN_MAX_BACKOFF_MS", options.MaxBackoffMs),
            SchedulerTickMs = ReadNullableIntEnv("KIOSK_ADMIN_SCHEDULER_TICK_MS", options.SchedulerTickMs)
        };

        return options.Normalize();
    }

    private static int ReadIntEnv(string key, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
    }

    private static int? ReadNullableIntEnv(string key, int? fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (int.TryParse(value, out var parsed) && parsed > 0)
            return parsed;

        return fallback;
    }

    private sealed class RuntimeOptionsJson
    {
        public int? DefaultPollingMs { get; set; }
        public int? MinPollingMs { get; set; }
        public int? MaxBackoffMs { get; set; }
        public int? SchedulerTickMs { get; set; }
    }
}
