using KIOSK.Device.Abstractions;

namespace KIOSK.Application.Services.Devices
{
    public sealed record DeviceStatusInfo(
        string DeviceId,
        string Name,
        string Vendor,
        string Model,
        string TransportType,
        string TransportPort,
        string TransportParam,
        string ProtocolName,
        int PollingMs,
        string DeviceType,
        string Driver);

    public interface IDeviceStatusService
    {
        event Action<string, StatusSnapshot>? StatusUpdated;

        IReadOnlyCollection<StatusSnapshot> GetAllSnapshots();
        StatusSnapshot? TryGet(string name);
        IReadOnlyList<DeviceStatusInfo> GetDevices();
        bool TryGetDevice(string deviceId, out DeviceStatusInfo info);
    }
}
