using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;

namespace KIOSK.Devices.Management
{
    internal sealed class SupervisorSession
    {
        private readonly DeviceDescriptor _desc;
        private readonly ITransportFactory _transportFactory;
        private readonly IDeviceFactory _deviceFactory;

        public event Action? TransportDisconnected;

        public SupervisorSession(DeviceDescriptor desc, ITransportFactory transportFactory, IDeviceFactory deviceFactory)
        {
            _desc = desc ?? throw new ArgumentNullException(nameof(desc));
            _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
            _deviceFactory = deviceFactory ?? throw new ArgumentNullException(nameof(deviceFactory));
        }

        public ITransport? Transport { get; private set; }
        public IDevice? Device { get; private set; }

        public async Task<StatusSnapshot> StartAsync(CancellationToken ct)
        {
            Transport = _transportFactory.Create(_desc);
            Transport.Disconnected += HandleTransportDisconnected;

            Device = _deviceFactory.Create(_desc, Transport);

            await Transport.OpenAsync(ct).ConfigureAwait(false);
            return await Device.InitializeAsync(ct).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken ct)
        {
            await CleanupDeviceAsync().ConfigureAwait(false);
            await CleanupTransportAsync(ct).ConfigureAwait(false);
        }

        private void HandleTransportDisconnected(object? sender, EventArgs e)
        {
            TransportDisconnected?.Invoke();
        }

        private async Task CleanupDeviceAsync()
        {
            try
            {
                if (Device is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (Device is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                Device = null;
            }
        }

        private async Task CleanupTransportAsync(CancellationToken ct)
        {
            try
            {
                if (Transport is not null)
                    await Transport.CloseAsync(ct).ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await (Transport?.DisposeAsync() ?? ValueTask.CompletedTask);
            }
            catch
            {
            }
            finally
            {
                if (Transport is not null)
                    Transport.Disconnected -= HandleTransportDisconnected;
                Transport = null;
            }
        }
    }
}
