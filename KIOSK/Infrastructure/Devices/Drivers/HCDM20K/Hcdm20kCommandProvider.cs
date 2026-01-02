using System.Collections.Generic;
using KIOSK.Device.Abstractions;

namespace KIOSK.Devices.Drivers.HCDM20K;

public sealed class Hcdm20kCommandProvider : ICommandProvider
{
    public string Model => "HCDM20K";

    public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands()
    {
        return new[]
        {
            new DeviceCommandDescriptor("RESTART", "재시작"),
            new DeviceCommandDescriptor("SENSOR", "센서 상태 조회"),
            new DeviceCommandDescriptor("INIT", "초기화"),
            new DeviceCommandDescriptor("VERSION", "버전 조회"),
            new DeviceCommandDescriptor("EJECT", "지폐 회수"),
            new DeviceCommandDescriptor("DISPENSE", "지폐 방출")
        };
    }
}
