namespace Kiosk.Infrastructure.Updates;

public sealed class VelopackOptions
{
    private const int DefaultPeriodicCheckMinutes = 60;
    private const int DefaultIdleApplySeconds = 60;

    public string? FeedUrl { get; init; }
    public string? ExplicitChannel { get; init; }
    public TimeSpan PeriodicCheckInterval { get; init; } = TimeSpan.FromMinutes(DefaultPeriodicCheckMinutes);
    public TimeSpan IdleApplyThreshold { get; init; } = TimeSpan.FromSeconds(DefaultIdleApplySeconds);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(FeedUrl);

    public static VelopackOptions LoadFromEnvironment()
    {
        return new VelopackOptions
        {
            FeedUrl = ReadValue("KIOSK_UPDATE_FEED_URL"),
            ExplicitChannel = ReadValue("KIOSK_UPDATE_CHANNEL"),
            PeriodicCheckInterval = TimeSpan.FromMinutes(ReadInt("KIOSK_UPDATE_CHECK_MINUTES", DefaultPeriodicCheckMinutes)),
            IdleApplyThreshold = TimeSpan.FromSeconds(ReadInt("KIOSK_UPDATE_IDLE_SECONDS", DefaultIdleApplySeconds))
        };
    }

    private static string? ReadValue(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int ReadInt(string key, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : fallback;
    }
}
