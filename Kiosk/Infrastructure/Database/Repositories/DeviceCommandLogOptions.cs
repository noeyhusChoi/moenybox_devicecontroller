namespace Kiosk.Infrastructure.Database.Repositories;

public sealed class DeviceCommandLogOptions
{
    public bool Enabled { get; set; } = true;

    // Direct | Buffered
    public string Mode { get; set; } = "Direct";

    public int QueueCapacity { get; set; } = 1000;

    public bool DropWhenFull { get; set; } = true;

    public int StopFlushTimeoutMs { get; set; } = 5000;
}

