namespace KIOSK.DeviceCommon.Devices;

public interface IDeviceRuntimeStatusEvents
{
    event Action<DeviceStatusSnapshot> StatusChanged;
}
