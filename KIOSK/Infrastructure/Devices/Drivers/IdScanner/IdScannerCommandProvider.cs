using System.Collections.Generic;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers.IdScanner;

internal sealed class IdScannerCommandProvider : ICommandProvider
{
    public string Model => "IDSCANNER";

    public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands() => new[]
    {
        new DeviceCommandDescriptor("RESTART", "재시작"),
        new DeviceCommandDescriptor("SCANSTART", "스캔 시작"),
        new DeviceCommandDescriptor("SCANSTOP", "스캔 중지"),
        new DeviceCommandDescriptor("GETSCANSTATUS", "스캔 상태 조회"),
        new DeviceCommandDescriptor("SAVEIMAGE", "이미지 저장"),
    };
}
