namespace KIOSK.Application.Services.Devices
{
    public sealed record DeviceCommandInfo(string Name, string Description = "");

    public interface IDeviceCommandCatalogService
    {
        IEnumerable<DeviceCommandInfo> GetFor(string deviceName);
    }
}
