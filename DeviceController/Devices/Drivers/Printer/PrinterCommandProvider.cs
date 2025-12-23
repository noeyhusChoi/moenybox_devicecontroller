using System.Collections.Generic;
using KIOSK.Devices.Management;

namespace KIOSK.Device.Drivers.Printer;

internal sealed class PrinterCommandProvider : IDeviceCommandProvider
{
    public string Model => "PRINTER";

    public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands() => new[]
    {
        new DeviceCommandDescriptor("PRINTCONTENT", "본문 인쇄"),
        new DeviceCommandDescriptor("PRINTTITLE", "제목 인쇄"),
        new DeviceCommandDescriptor("CUT", "용지 컷"),
        new DeviceCommandDescriptor("QR", "QR 코드 인쇄"),
        new DeviceCommandDescriptor("ALIGN", "정렬 설정"),
    };
}
