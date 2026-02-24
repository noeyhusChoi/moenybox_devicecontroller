using CommunityToolkit.Mvvm.Messaging.Messages;
using KIOSK.DeviceCommon.Devices;

namespace KIOSK.Admin.Messages;

public sealed class DeviceStatusChangedMessage : ValueChangedMessage<DeviceStatusSnapshot>
{
    public DeviceStatusChangedMessage(DeviceStatusSnapshot value) : base(value)
    {
    }
}

