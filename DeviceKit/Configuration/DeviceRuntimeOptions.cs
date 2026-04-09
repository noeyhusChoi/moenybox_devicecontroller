namespace DeviceKit.Engine;

public sealed record DeviceRuntimeOptions
{
    public static DeviceRuntimeOptions Default { get; } = new();

    public int DefaultPollingMs { get; init; } = 10000;
    public int MinPollingMs { get; init; } = 1000;
    public int MaxBackoffMs { get; init; } = 60000;
    public int? SchedulerTickMs { get; init; }

    public DeviceRuntimeOptions Normalize()
    {
        var minPolling = Math.Max(100, MinPollingMs);
        var defaultPolling = Math.Max(minPolling, DefaultPollingMs);
        var maxBackoff = Math.Max(defaultPolling, MaxBackoffMs);

        int? schedulerTick = null;
        if (SchedulerTickMs is > 0)
            schedulerTick = Math.Max(100, SchedulerTickMs.Value);

        return this with
        {
            MinPollingMs = minPolling,
            DefaultPollingMs = defaultPolling,
            MaxBackoffMs = maxBackoff,
            SchedulerTickMs = schedulerTick
        };
    }
}
