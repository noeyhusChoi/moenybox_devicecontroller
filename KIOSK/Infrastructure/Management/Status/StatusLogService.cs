using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Cache;
using KIOSK.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace KIOSK.Infrastructure.Management.Status;

public interface IStatusLogService
{
    Task SaveAsync(string name, StatusSnapshot snapshot);
}

public sealed class StatusLogService : IStatusLogService
{
    private readonly DeviceStatusLogRepository _repository;
    private readonly IMemoryCache _cache;

    public StatusLogService(DeviceStatusLogRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public Task SaveAsync(string name, StatusSnapshot snapshot)
    {
        var kiosks = _cache.Get<IReadOnlyList<KioskModel>>(DatabaseCacheKeys.Kiosk)
            ?? Array.Empty<KioskModel>();
        var kioskId = kiosks.FirstOrDefault()?.Id;
        if (string.IsNullOrWhiteSpace(kioskId))
            return Task.CompletedTask;

        var devices = _cache.Get<IReadOnlyList<DeviceModel>>(DatabaseCacheKeys.DeviceList)
            ?? Array.Empty<DeviceModel>();
        var deviceType = devices
            .FirstOrDefault(d =>
                string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.Id, name, StringComparison.OrdinalIgnoreCase))
            ?.DeviceType;
        if (string.IsNullOrWhiteSpace(deviceType))
            return Task.CompletedTask;

        return _repository.SaveAsync(kioskId, deviceType, name, snapshot);
    }
}
