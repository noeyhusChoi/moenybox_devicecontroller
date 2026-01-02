using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Database.Interface
{
    public interface IDeleteRepository<T>
    {
        Task DeleteAsync(object id, CancellationToken ct = default);
    }
}
