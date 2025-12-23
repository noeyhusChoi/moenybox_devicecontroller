using System.Collections.Generic;
using KIOSK.Devices.Management;

namespace KIOSK.Device.Drivers.E200Z;

internal sealed class E200ZCommandProvider : IDeviceCommandProvider
{
    public string Model => "QR_TOTINFO";

    public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands() => new[]
    {
        new DeviceCommandDescriptor("SCAN_ENABLE", "스캔 활성화"),
        new DeviceCommandDescriptor("SCAN_DISABLE", "스캔 비활성화"),
        new DeviceCommandDescriptor("START_DECODE", "디코드 시작"),
        new DeviceCommandDescriptor("STOP_DECODE", "디코드 중지"),
        new DeviceCommandDescriptor("RESET", "리셋"),
        new DeviceCommandDescriptor("SET_HOST_TRIGGER", "Host Trigger 모드"),
        new DeviceCommandDescriptor("SET_AUTO_TRIGGER", "Auto-Induction 모드"),
        new DeviceCommandDescriptor("SET_PACKET_MODE", "Packet 모드"),
        new DeviceCommandDescriptor("REQUEST_REVISION", "Revision 조회"),
    };
}
