using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Database.Repositories;

namespace KIOSK.Infrastructure.Management.Status;

public interface IStatusLogService
{
    Task SaveAsync(string name, StatusSnapshot snapshot);
}

public sealed class StatusLogService : IStatusLogService
{
    private readonly DeviceStatusLogRepository _repository;

    public StatusLogService(DeviceStatusLogRepository repository)
    {
        _repository = repository;
    }

    public Task SaveAsync(string name, StatusSnapshot snapshot)
        => _repository.SaveAsync(name, snapshot);
}
