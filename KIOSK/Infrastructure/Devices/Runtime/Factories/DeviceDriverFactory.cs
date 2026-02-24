// Core/DeviceFactory.cs
using System;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Device.Transport;

namespace KIOSK.Infrastructure.Devices.Runtime.Factories;

public interface IDeviceDriverFactory
{
    IDeviceDriver Create(DeviceDescriptor descriptor, ITransport transport);
}

/// <summary>
/// 장치 정의(Descriptor) -> 실제 인스턴스 생성 팩토리
/// </summary>
public sealed class DeviceDriverFactory : IDeviceDriverFactory
{
    public DeviceDriverFactory()
    {
    }

    public IDeviceDriver Create(DeviceDescriptor descriptor, ITransport transport)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Driver))
            throw new NotSupportedException(
                $"driver_type is required. name={descriptor.Name} deviceType={descriptor.DeviceType} vendor={descriptor.Vendor} model={descriptor.Model} transport={descriptor.TransportType}:{descriptor.TransportPort}/{descriptor.TransportParam}");

        if (TryCreateByDriverType(descriptor, transport, out var device))
            return device;

        throw new NotSupportedException(
            $"Ready driver_type: {descriptor.Driver}. name={descriptor.Name} deviceType={descriptor.DeviceType} vendor={descriptor.Vendor} model={descriptor.Model}");
    }

    private static bool TryCreateByDriverType(DeviceDescriptor descriptor, ITransport transport, out IDeviceDriver device)
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
                    $"Ready driver_type: {descriptor.Driver} (normalized={driverType}). name={descriptor.Name} deviceType={descriptor.DeviceType} vendor={descriptor.Vendor} model={descriptor.Model}");
        }
    }

    private static string NormalizeDriverType(string? driverType)
        => (driverType ?? string.Empty).Trim().ToUpperInvariant();

}
