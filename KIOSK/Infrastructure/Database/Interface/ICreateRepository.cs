using System.Threading;
using System.Threading.Tasks;

namespace Kiosk.Infrastructure.Database.Interface
{
    public interface ICreateRepository<T>
    {
        Task InsertAsync(T entity, CancellationToken ct = default);
    }
}
