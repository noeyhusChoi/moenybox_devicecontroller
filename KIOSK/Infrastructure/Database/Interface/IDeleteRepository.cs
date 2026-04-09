using System.Threading;
using System.Threading.Tasks;

namespace Kiosk.Infrastructure.Database.Interface
{
    public interface IDeleteRepository<T>
    {
        Task DeleteAsync(object id, CancellationToken ct = default);
    }
}
