using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Database.Interface
{
    public interface IUpdateRepository<T>
    {
        Task UpdateAsync(IReadOnlyList<T> entities, CancellationToken ct = default);
    }
}
