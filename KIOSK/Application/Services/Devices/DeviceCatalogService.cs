using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Repositories;

namespace KIOSK.Application.Services.Devices
{
    public sealed class DeviceCatalogService : IDeviceCatalogService
    {
        private readonly DeviceRepository _repo;

        public DeviceCatalogService(DeviceRepository repo)
        {
            _repo = repo;
        }

        public Task<IReadOnlyList<DeviceModel>> LoadAllAsync(CancellationToken ct = default)
            => _repo.LoadAllAsync(ct);
    }
}
