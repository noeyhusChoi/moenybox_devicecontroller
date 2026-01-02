using System.Collections.Generic;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers.EM20;

public sealed class Em20CommandProvider : ICommandProvider
{
    public string Model => "QR_NEWLAND";

    public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands()
    {
        return new[]
        {
            new DeviceCommandDescriptor("RESTART", "재시작"),
            new DeviceCommandDescriptor("SCAN.ONCE", "QR 단일 스캔"),
            new DeviceCommandDescriptor("SCAN.MANY", "QR 다중 스캔"),
            new DeviceCommandDescriptor("SCAN.TRIGGERON", "트리거 ON"),
            new DeviceCommandDescriptor("SCAN.TRIGGEROFF", "트리거 OFF"),
            new DeviceCommandDescriptor("SCAN.READ", "버퍼 읽기")
        };
    }
}
