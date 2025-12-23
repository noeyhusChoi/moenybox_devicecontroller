using System.Collections.Generic;

namespace KIOSK.Devices.Management;

public interface IDeviceCommandProvider
{
    string Model { get; }
    IReadOnlyCollection<DeviceCommandDescriptor> GetCommands();
}
