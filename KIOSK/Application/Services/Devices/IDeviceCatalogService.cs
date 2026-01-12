using KIOSK.Domain.Entities;

namespace KIOSK.Application.Services.Devices
{
    public interface IDeviceCatalogService
    {
        Task<IReadOnlyList<DeviceModel>> LoadAllAsync(CancellationToken ct = default);
    }
}
