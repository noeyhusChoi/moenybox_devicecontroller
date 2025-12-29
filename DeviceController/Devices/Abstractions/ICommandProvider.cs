using System.Collections.Generic;

namespace KIOSK.Device.Abstractions;

public interface ICommandProvider
{
    string Model { get; }
    IReadOnlyCollection<DeviceCommandDescriptor> GetCommands();
}
