// Core/DeviceRegistry.cs
using System;
using System.Collections.Generic;
using System.Linq;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Device.Transport;
using KIOSK.Devices.Drivers;

namespace KIOSK.Device.Core;

public interface IDeviceFactory
{
    IDevice Create(DeviceDescriptor descriptor, ITransport transport);
}

public interface IDeviceFactoryContributor
{
    bool CanCreate(DeviceDescriptor descriptor);
    IDevice Create(DeviceDescriptor descriptor, ITransport transport);
}

/// <summary>
/// 장치 정의(Descriptor) -> 실제 인스턴스 생성 팩토리
/// </summary>
public sealed class DeviceFactory : IDeviceFactory
{
    private readonly IReadOnlyDictionary<string, Func<DeviceDescriptor, ITransport, IDevice>> _builtIns;
    private readonly IReadOnlyCollection<IDeviceFactoryContributor> _contributors;

    public DeviceFactory(IEnumerable<IDeviceFactoryContributor>? contributors = null)
    {
        _contributors = contributors?.ToArray() ?? Array.Empty<IDeviceFactoryContributor>();
        _builtIns = new Dictionary<string, Func<DeviceDescriptor, ITransport, IDevice>>(StringComparer.OrdinalIgnoreCase)
        {
            ["PRINTER"] = (d, t) => new DevicePrinter(d, t),
            ["QR_NEWLAND"] = (d, t) => new DeviceQrEM20(d, t),
            ["QR_TOTINFO"] = (d, t) => new DeviceQrE200Z(d, t),
            ["IDSCANNER"] = (d, t) => new DeviceIdScanner(d, t),
            ["HCDM10K"] = (d, t) => new DeviceHCDM10K(d, t),
            ["HCDM20K"] = (d, t) => new DeviceHCDM20K(d, t),
            ["DEPOSIT"] = (d, t) => new DeviceDeposit(d, t),
        };
    }

    public IDevice Create(DeviceDescriptor descriptor, ITransport transport)
    {
        foreach (var contributor in _contributors)
        {
            if (contributor.CanCreate(descriptor))
                return contributor.Create(descriptor, transport);
        }

        if (_builtIns.TryGetValue(descriptor.Model, out var factory))
            return factory(descriptor, transport);

        throw new NotSupportedException($"Unknown model: {descriptor.Model}");
    }
}
