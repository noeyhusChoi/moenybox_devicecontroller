using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Status;

public interface IStatusRepository
{
    Task SaveAsync(string name, StatusSnapshot snapshot);
}

public sealed class NullStatusRepository : IStatusRepository
{
    public Task SaveAsync(string name, StatusSnapshot snapshot) => Task.CompletedTask;
}
