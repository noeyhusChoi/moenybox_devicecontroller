using System.Threading;
using System.Threading.Tasks;

namespace Kiosk.Infrastructure.Database.Interface
{
    public interface IUpdateRepository<T>
    {
        Task UpdateAsync(IReadOnlyList<T> entities, CancellationToken ct = default);
    }
}
