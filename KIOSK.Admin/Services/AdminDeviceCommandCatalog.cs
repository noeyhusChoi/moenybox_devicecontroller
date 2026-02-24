using System;
using System.Collections.Generic;
using System.Linq;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime.Factories;

namespace KIOSK.Admin.Services;

public sealed record AdminDeviceCommandItem(string Name, string Description)
{
    public string Display => string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} - {Description}";
}

public interface IAdminDeviceCommandCatalog
{
    IReadOnlyList<AdminDeviceCommandItem> GetByDevice(string? deviceId, string? deviceType = null);
    IReadOnlyList<AdminDeviceCommandItem> GetByDeviceType(string? deviceType);
}

public sealed class AdminDeviceCommandCatalog : IAdminDeviceCommandCatalog
{
    private static readonly IReadOnlyList<AdminDeviceCommandItem> Empty = Array.Empty<AdminDeviceCommandItem>();
    private readonly IDeviceDriverCommandMetadataProvider _metadataProvider;
    private readonly IReadOnlyDictionary<string, string> _driverByDeviceId;
    private readonly IReadOnlyDictionary<string, string> _firstDriverByDeviceType;
    private readonly Dictionary<string, IReadOnlyList<AdminDeviceCommandItem>> _cacheByDriver
        = new(StringComparer.OrdinalIgnoreCase);

    public AdminDeviceCommandCatalog(
        IEnumerable<DeviceDescriptor> descriptors,
        IDeviceDriverCommandMetadataProvider? metadataProvider = null)
    {
        _metadataProvider = metadataProvider ?? new DeviceDriverCommandMetadataProvider();
        var descriptorList = descriptors.ToList();

        _driverByDeviceId = descriptorList
            .Where(d => !string.IsNullOrWhiteSpace(d.EffectiveId))
            .GroupBy(d => d.EffectiveId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First().Driver,
                StringComparer.OrdinalIgnoreCase);

        _firstDriverByDeviceType = descriptorList
            .Where(d => !string.IsNullOrWhiteSpace(d.DeviceType))
            .GroupBy(d => d.DeviceType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Driver).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AdminDeviceCommandItem> GetByDevice(string? deviceId, string? deviceType = null)
    {
        if (!string.IsNullOrWhiteSpace(deviceId) && _driverByDeviceId.TryGetValue(deviceId, out var driverType))
            return GetByDriverType(driverType);

        return GetByDeviceType(deviceType);
    }

    public IReadOnlyList<AdminDeviceCommandItem> GetByDeviceType(string? deviceType)
    {
        if (string.IsNullOrWhiteSpace(deviceType))
            return Empty;

        if (!_firstDriverByDeviceType.TryGetValue(deviceType, out var driverType))
            return Empty;

        return GetByDriverType(driverType);
    }

    private IReadOnlyList<AdminDeviceCommandItem> GetByDriverType(string? driverType)
    {
        if (string.IsNullOrWhiteSpace(driverType))
            return Empty;

        if (_cacheByDriver.TryGetValue(driverType, out var cached))
            return cached;

        var mapped = _metadataProvider
            .GetByDriverType(driverType)
            .Select(x => new AdminDeviceCommandItem(x.Name, x.Description))
            .ToArray();

        _cacheByDriver[driverType] = mapped;
        return mapped;
    }
}
