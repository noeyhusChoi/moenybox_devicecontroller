namespace Kiosk.Infrastructure.Updates;

public sealed class VelopackOptions
{
    private const string DefaultFeedUrl = "https://github.com/noeyhusChoi/moenybox_devicecontroller";
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
            FeedUrl = DefaultFeedUrl,
            ExplicitChannel = null,
            PeriodicCheckInterval = TimeSpan.FromMinutes(ReadInt("KIOSK_UPDATE_CHECK_MINUTES", DefaultPeriodicCheckMinutes)),
            IdleApplyThreshold = TimeSpan.FromSeconds(ReadInt("KIOSK_UPDATE_IDLE_SECONDS", DefaultIdleApplySeconds))
        };
    }

    private static int ReadInt(string key, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : fallback;
    }
}
