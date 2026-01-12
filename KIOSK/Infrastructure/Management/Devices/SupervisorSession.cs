using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Infrastructure.Management.Devices
{
    internal sealed class SupervisorSession
    {
        private readonly DeviceDescriptor _desc;
        private readonly ITransportFactory _transportFactory;
        private readonly IDeviceFactory _deviceFactory;
        private readonly ILogger _logger;

        public event Action? TransportDisconnected;

        public SupervisorSession(
            DeviceDescriptor desc,
            ITransportFactory transportFactory,
            IDeviceFactory deviceFactory,
            ILogger? logger = null)
        {
            _desc = desc ?? throw new ArgumentNullException(nameof(desc));
            _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
            _deviceFactory = deviceFactory ?? throw new ArgumentNullException(nameof(deviceFactory));
            _logger = logger ?? NullLogger.Instance;
        }

        public ITransport? Transport { get; private set; }
        public IDevice? Device { get; private set; }

        public async Task<StatusSnapshot> StartAsync(CancellationToken ct)
        {
            _logger.LogInformation(
                "Session start. device={Device} type={DeviceType} driver={Driver} comm={CommType} port={CommPort} param={CommParam}",
                _desc.Name,
                _desc.DeviceType,
                _desc.Driver,
                _desc.TransportType,
                _desc.TransportPort,
                _desc.TransportParam);

            try
            {
                Transport = _transportFactory.Create(_desc);
                Transport.Disconnected += HandleTransportDisconnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transport create failed. device={Device}", _desc.Name);
                throw;
            }

            try
            {
                Device = _deviceFactory.Create(_desc, Transport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device create failed. device={Device} driver={Driver}", _desc.Name, _desc.Driver);
                throw;
            }

            try
            {
                await Transport.OpenAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("Transport opened. device={Device}", _desc.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transport open failed. device={Device} port={CommPort}", _desc.Name, _desc.TransportPort);
                throw;
            }

            try
            {
                var snapshot = await Device.InitializeAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("Device initialized. device={Device}", _desc.Name);
                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device initialize failed. device={Device}", _desc.Name);
                throw;
            }
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
