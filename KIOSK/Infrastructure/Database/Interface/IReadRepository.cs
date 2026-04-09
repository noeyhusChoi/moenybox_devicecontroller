using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kiosk.Infrastructure.Database.Interface
{
    public interface IReadRepository<T>
    {
        //Task<T?> FindAsync(object id, CancellationToken ct = default);
        Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken ct = default);
    }
}
