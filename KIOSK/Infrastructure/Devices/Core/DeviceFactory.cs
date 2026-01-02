// Core/DeviceFactory.cs
using System;
using System.Collections.Generic;
using System.Linq;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Device.Transport;
using KIOSK.Devices.Drivers;
using Microsoft.Extensions.Logging;

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
    private readonly ILoggerFactory? _loggerFactory;

    public DeviceFactory(ILoggerFactory? loggerFactory = null, IEnumerable<IDeviceFactoryContributor>? contributors = null)
    {
        _loggerFactory = loggerFactory;
        _contributors = contributors?.ToArray() ?? Array.Empty<IDeviceFactoryContributor>();
        _builtIns = new Dictionary<string, Func<DeviceDescriptor, ITransport, IDevice>>(StringComparer.OrdinalIgnoreCase)
        {
            ["PRINTER"] = (d, t) => new PrinterDriver(d, t, _loggerFactory?.CreateLogger<PrinterDriver>()),
            ["QR_NEWLAND"] = (d, t) => new QrEM20Driver(d, t, _loggerFactory?.CreateLogger<QrEM20Driver>()),
            ["QR_TOTINFO"] = (d, t) => new QrE200ZDriver(d, t, _loggerFactory?.CreateLogger<QrE200ZDriver>()),
            ["IDSCANNER"] = (d, t) => new IdScannerDriver(d, t, _loggerFactory?.CreateLogger<IdScannerDriver>()),
            ["HCDM10K"] = (d, t) => new Hcdm10kDriver(d, t, _loggerFactory?.CreateLogger<Hcdm10kDriver>()),
            ["HCDM20K"] = (d, t) => new Hcdm20kDriver(d, t, _loggerFactory?.CreateLogger<Hcdm20kDriver>()),
            ["DEPOSIT"] = (d, t) => new DepositDriver(d, t, _loggerFactory?.CreateLogger<DepositDriver>()),
        };
    }

    public IDevice Create(DeviceDescriptor descriptor, ITransport transport)
    {
        foreach (var contributor in _contributors)
        {
            if (contributor.CanCreate(descriptor))
                return contributor.Create(descriptor, transport);
        }

        var driverKey = DeviceDriverResolver.Resolve(descriptor);
        if (_builtIns.TryGetValue(driverKey, out var factory))
            return factory(descriptor, transport);

        throw new NotSupportedException($"Unknown driver: {driverKey} (vendor={descriptor.Vendor}, model={descriptor.Model})");
    }
}
