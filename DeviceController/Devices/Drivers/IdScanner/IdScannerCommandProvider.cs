using System.Collections.Generic;
using KIOSK.Devices.Management;

namespace KIOSK.Device.Drivers.IdScanner;

internal sealed class IdScannerCommandProvider : IDeviceCommandProvider
{
    public string Model => "IDSCANNER";

    public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands() => new[]
    {
        new DeviceCommandDescriptor("SCANSTART", "스캔 시작"),
        new DeviceCommandDescriptor("SCANSTOP", "스캔 중지"),
        new DeviceCommandDescriptor("GETSCANSTATUS", "스캔 상태 조회"),
        new DeviceCommandDescriptor("SAVEIMAGE", "이미지 저장"),
    };
}
