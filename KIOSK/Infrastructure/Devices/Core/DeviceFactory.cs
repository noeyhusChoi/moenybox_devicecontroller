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
    private readonly IReadOnlyCollection<IDeviceFactoryContributor> _contributors;
    private readonly ILoggerFactory? _loggerFactory;

    public DeviceFactory(ILoggerFactory? loggerFactory = null, IEnumerable<IDeviceFactoryContributor>? contributors = null)
    {
        _loggerFactory = loggerFactory;
        _contributors = contributors?.ToArray() ?? Array.Empty<IDeviceFactoryContributor>();
    }

    public IDevice Create(DeviceDescriptor descriptor, ITransport transport)
    {
        foreach (var contributor in _contributors)
        {
            if (contributor.CanCreate(descriptor))
                return contributor.Create(descriptor, transport);
        }

        if (string.IsNullOrWhiteSpace(descriptor.Driver))
            throw new NotSupportedException(
                $"driver_type is required. name={descriptor.Name} deviceType={descriptor.DeviceType} vendor={descriptor.Vendor} model={descriptor.Model} transport={descriptor.TransportType}:{descriptor.TransportPort}/{descriptor.TransportParam}");

        if (TryCreateByDriverType(descriptor, transport, out var device))
            return device;

        throw new NotSupportedException(
            $"Unknown driver_type: {descriptor.Driver}. name={descriptor.Name} deviceType={descriptor.DeviceType} vendor={descriptor.Vendor} model={descriptor.Model}");
    }

    private static bool TryCreateByDriverType(DeviceDescriptor descriptor, ITransport transport, out IDevice device)
    {
        device = null!;
        var driverType = NormalizeDriverType(descriptor.Driver);
        if (string.IsNullOrWhiteSpace(driverType))
            return false;

        switch (driverType)
        {
            case "E200Z":
                device = new QrE200ZDriver(descriptor, transport);
                return true;
            case "EM20-80":
                device = new QrEM20Driver(descriptor, transport);
                return true;
            case "HCDM10K":
                device = new Hcdm10kDriver(descriptor, transport);
                return true;
            case "HCDM20K":
                device = new Hcdm20kDriver(descriptor, transport);
                return true;
            case "HMK-072":
                device = new PrinterDriver(descriptor, transport);
                return true;
            case "COMBOSCAN2208":
                device = new IdScannerDriver(descriptor, transport);
                return true;
            case "SC8307":
                device = new DepositDriver(descriptor, transport);
                return true;
            default:
                throw new NotSupportedException(
                    $"Unknown driver_type: {descriptor.Driver} (normalized={driverType}). name={descriptor.Name} deviceType={descriptor.DeviceType} vendor={descriptor.Vendor} model={descriptor.Model}");
        }
    }

    private static string NormalizeDriverType(string? driverType)
        => (driverType ?? string.Empty).Trim().ToUpperInvariant();

}
