using System.Threading;
using System.Threading.Tasks;

namespace Kiosk.Infrastructure.Database.Repositories;

public interface IDeviceCommandLogSink
{
    Task WriteAsync(DeviceCommandRecord record, CancellationToken ct = default);
}

