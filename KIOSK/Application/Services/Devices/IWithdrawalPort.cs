using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Devices
{
    public interface IWithdrawalPort
    {
        Task<Dictionary<int, (int req, int exit, int rej)>?> DispenseAsync(
            string deviceId,
            byte[] payload,
            CancellationToken ct);
    }
}
