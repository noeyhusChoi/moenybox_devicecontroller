using System;
using System.Collections.Generic;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Device.Drivers.Deposit;
using KIOSK.Device.Drivers.HCDM;
using KIOSK.Device.Drivers.HCDM20K;
using KIOSK.Device.Drivers.IdScanner;
using KIOSK.Device.Drivers.Printer;

namespace KIOSK.Infrastructure.Devices.Runtime.Factories;

public interface IDeviceDriverCommandMetadataProvider
{
    IReadOnlyCollection<DeviceCommandDescriptor> GetByDriverType(string? driverType);
}

public sealed class DeviceDriverCommandMetadataProvider : IDeviceDriverCommandMetadataProvider
{
    private static readonly IReadOnlyCollection<DeviceCommandDescriptor> Empty = Array.Empty<DeviceCommandDescriptor>();

    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> _byDriverType
        = new Dictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>>(StringComparer.OrdinalIgnoreCase)
        {
            ["E200Z"] = QrE200ZDriver.SupportedCommands,
            ["EM20-80"] = QrEM20Driver.SupportedCommands,
            ["HMK-072"] = PrinterCommandHandlers.SupportedCommands,
            ["COMBOSCAN2208"] = IdScannerCommandHandlers.SupportedCommands,
            ["SC8307"] = DepositCommandHandlers.SupportedCommands,
            ["HCDM10K"] = Hcdm10kCommandHandlers.SupportedCommands,
            ["HCDM20K"] = Hcdm20kCommandHandlers.SupportedCommands,
        };

    public IReadOnlyCollection<DeviceCommandDescriptor> GetByDriverType(string? driverType)
    {
        if (string.IsNullOrWhiteSpace(driverType))
            return Empty;

        var key = driverType.Trim().ToUpperInvariant();
        return _byDriverType.TryGetValue(key, out var commands) ? commands : Empty;
    }
}
