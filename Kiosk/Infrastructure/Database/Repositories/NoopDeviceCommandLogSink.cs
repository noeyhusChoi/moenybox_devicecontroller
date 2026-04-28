using System.Threading;
using System.Threading.Tasks;

namespace Kiosk.Infrastructure.Database.Repositories;

public sealed class NoopDeviceCommandLogSink : IDeviceCommandLogSink
{
    public Task WriteAsync(DeviceCommandRecord record, CancellationToken ct = default)
        => Task.CompletedTask;
}

