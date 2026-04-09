using DeviceKit.Commands;

namespace DeviceKit.Composition;

internal sealed class DeviceDriverHandle : IAsyncDisposable
{
    public DeviceDriverHandle(
        IDeviceDriver driver,
        IReadOnlyCollection<DeviceCommandSpec> commands)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        Commands = commands ?? Array.Empty<DeviceCommandSpec>();
    }

    public IDeviceDriver Driver { get; }
    public IReadOnlyCollection<DeviceCommandSpec> Commands { get; }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Driver is IAsyncDisposable asyncDriver)
                await asyncDriver.DisposeAsync().ConfigureAwait(false);
            else if (Driver is IDisposable disposableDriver)
                disposableDriver.Dispose();
        }
        catch
        {
        }

    }
}
