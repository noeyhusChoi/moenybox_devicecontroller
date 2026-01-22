using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeSelectLanguageUseCase
    {
        Task SelectAsync(string? selection, CancellationToken ct = default);
    }
}
