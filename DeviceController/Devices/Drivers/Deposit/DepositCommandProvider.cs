using System.Collections.Generic;
using KIOSK.Devices.Management;

namespace KIOSK.Device.Drivers.Deposit;

internal sealed class DepositCommandProvider : IDeviceCommandProvider
{
    public string Model => "DEPOSIT";

    public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands() => new[]
    {
        new DeviceCommandDescriptor("START", "입금 시작"),
        new DeviceCommandDescriptor("STOP", "입금 중지"),
        new DeviceCommandDescriptor("STACK", "스택 처리"),
        new DeviceCommandDescriptor("RETURN", "리턴 처리"),
    };
}
