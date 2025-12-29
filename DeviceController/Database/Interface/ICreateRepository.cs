using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Database.Interface
{
    public interface ICreateRepository<T>
    {
        Task InsertAsync(T entity, CancellationToken ct = default);
    }
}
