using System;
using System.Collections.Generic;
using System.Linq;
using KIOSK.Device.Abstractions;
using KIOSK.Devices.Management;

namespace KIOSK.Device.Abstractions;

public sealed record DeviceCommandDescriptor(string Name, string Description = "");

public interface IDeviceCommandCatalog
{
    IReadOnlyCollection<DeviceCommandDescriptor> GetFor(string deviceName);
    IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAll();
}

/// <summary>
/// "장치 모델" 기준으로 UI에 노출할 명령 목록을 제공한다.
/// (장치 코드와 분리해서, UI가 장치 내부 구현에 직접 의존하지 않도록 한다.)
/// </summary>
public sealed class DeviceCommandCatalog : IDeviceCommandCatalog
{
    private readonly IDeviceHost _runtime;
    private readonly IReadOnlyDictionary<string, ICommandProvider> _providers;

    public DeviceCommandCatalog(
        IDeviceHost runtime,
        IEnumerable<ICommandProvider> providers)
    {
        _runtime = runtime;
        _providers = providers
            .GroupBy(p => p.Model, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<DeviceCommandDescriptor> GetFor(string deviceName)
    {
        if (!_runtime.TryGetSupervisor(deviceName, out var sup))
            return Array.Empty<DeviceCommandDescriptor>();

        return GetByModel(sup.Model);
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAll()
    {
        return _runtime.GetAllSupervisors()
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(s => s.Name, s => (IReadOnlyCollection<DeviceCommandDescriptor>)GetByModel(s.Model), StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyCollection<DeviceCommandDescriptor> GetByModel(string model)
    {
        if (_providers.TryGetValue(model, out var provider))
            return provider.GetCommands();

        return Array.Empty<DeviceCommandDescriptor>();
    }
}
