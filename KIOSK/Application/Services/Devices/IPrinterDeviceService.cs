using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Devices
{
    public interface IPrinterDeviceService
    {
        Task PrintTitleAsync(string deviceId, string content, CancellationToken ct = default);
        Task PrintContentAsync(string deviceId, string content, CancellationToken ct = default);
        Task CutAsync(string deviceId, CancellationToken ct = default);
    }
}
