using System.Collections.Generic;

namespace KIOSK.Device.Abstractions;

public sealed record DeviceCommandDescriptor(string Name, string Description = "");

public interface IDeviceCommandMetadataProvider
{
    IReadOnlyCollection<DeviceCommandDescriptor> SupportedCommands { get; }
}
