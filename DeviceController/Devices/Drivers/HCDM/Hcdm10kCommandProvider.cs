using System.Collections.Generic;
using KIOSK.Device.Abstractions;

namespace KIOSK.Devices.Drivers.HCDM;

internal sealed class Hcdm10kCommandProvider : ICommandProvider
{
    public string Model => "HCDM10K";

    public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands() => new[]
    {
        new DeviceCommandDescriptor("RESTART", "재시작"),
        new DeviceCommandDescriptor("SENSOR", "센서 조회"),
        new DeviceCommandDescriptor("INIT", "초기화"),
        new DeviceCommandDescriptor("DISPENSE", "지폐 방출"),
        new DeviceCommandDescriptor("EJECT", "방출/회수"),
    };
}
