using System;
using System.Linq;
using DeviceKit.Commands;
using Microsoft.Extensions.Logging;

namespace DeviceKit.Composition;

internal static class DeviceDriverRegistry
{
    public static DeviceDriverHandle CreateHandle(DeviceDescriptor descriptor, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (string.IsNullOrWhiteSpace(descriptor.DriverType))
            throw new NotSupportedException("driver_type is required.");

        return NormalizeDriverType(descriptor.DriverType) switch
        {
            "E200Z" => new DeviceDriverHandle(new QrE200ZDriver(descriptor), QrE200ZDriver.CommandTable.Values.ToArray()),
            "EM20-80" => new DeviceDriverHandle(new QrEM20Driver(descriptor), QrEM20Driver.CommandTable.Values.ToArray()),
            "HCDM10K" => new DeviceDriverHandle(new Hcdm10kDriver(descriptor, loggerFactory?.CreateLogger<Hcdm10kDriver>()), Hcdm10kDriver.CommandTable.Values.ToArray()),
            "HCDM20K" => new DeviceDriverHandle(new Hcdm20kDriver(descriptor, loggerFactory?.CreateLogger<Hcdm20kDriver>()), Hcdm20kDriver.CommandTable.Values.ToArray()),
            "LCDM4000" => new DeviceDriverHandle(new Lcdm4000Driver(descriptor, loggerFactory?.CreateLogger<Lcdm4000Driver>()), Lcdm4000Driver.CommandTable.Values.ToArray()),
            "LCDM-4000" => new DeviceDriverHandle(new Lcdm4000Driver(descriptor, loggerFactory?.CreateLogger<Lcdm4000Driver>()), Lcdm4000Driver.CommandTable.Values.ToArray()),
            "HMK-072" => new DeviceDriverHandle(new PrinterDriver(descriptor), PrinterDriver.CommandTable.Values.ToArray()),
            "COMBOSCAN2208" => new DeviceDriverHandle(new IdScannerDriver(descriptor, loggerFactory?.CreateLogger<IdScannerDriver>()), IdScannerDriver.CommandTable.Values.ToArray()),
            "SC8307" => new DeviceDriverHandle(new DepositDriver(descriptor, loggerFactory?.CreateLogger<DepositDriver>()), DepositDriver.CommandTable.Values.ToArray()),
            _ => throw new NotSupportedException($"Ready driver_type: {descriptor.DriverType}")
        };
    }

    public static IReadOnlyCollection<DeviceCommandSpec> GetSupportedCommands(string? driverType)
    {
        if (string.IsNullOrWhiteSpace(driverType))
            return Array.Empty<DeviceCommandSpec>();

        return NormalizeDriverType(driverType) switch
        {
            "E200Z" => QrE200ZDriver.CommandTable.Values.ToArray(),
            "EM20-80" => QrEM20Driver.CommandTable.Values.ToArray(),
            "HCDM10K" => Hcdm10kDriver.CommandTable.Values.ToArray(),
            "HCDM20K" => Hcdm20kDriver.CommandTable.Values.ToArray(),
            "LCDM4000" => Lcdm4000Driver.CommandTable.Values.ToArray(),
            "LCDM-4000" => Lcdm4000Driver.CommandTable.Values.ToArray(),
            "HMK-072" => PrinterDriver.CommandTable.Values.ToArray(),
            "COMBOSCAN2208" => IdScannerDriver.CommandTable.Values.ToArray(),
            "SC8307" => DepositDriver.CommandTable.Values.ToArray(),
            _ => Array.Empty<DeviceCommandSpec>()
        };
    }

    private static string NormalizeDriverType(string? driverType)
        => (driverType ?? string.Empty).Trim().ToUpperInvariant();
}
