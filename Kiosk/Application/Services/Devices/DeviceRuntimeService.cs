using DeviceKit.Configuration;
using DeviceKit.Engine;
using Kiosk.Infrastructure.Database.Models;
using Kiosk.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Logging;

namespace Kiosk.Application.Services.Devices;

public sealed class DeviceRuntimeService : IDeviceRuntimeService, IAsyncDisposable
{
    private readonly DeviceRepository _deviceRepository;
    private readonly ILogger<DeviceRuntimeService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DeviceRuntimePort? _runtime;

    public DeviceRuntimeService(
        DeviceRepository deviceRepository,
        ILogger<DeviceRuntimeService> logger,
        ILoggerFactory loggerFactory)
    {
        _deviceRepository = deviceRepository;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<IDeviceManagerPort> GetPortAsync(CancellationToken ct = default)
    {
        if (_runtime is not null)
            return _runtime;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_runtime is not null)
                return _runtime;

            var devices = await _deviceRepository.LoadAllAsync(ct).ConfigureAwait(false);
            var descriptors = devices.Select(MapToDescriptor).ToArray();

            _runtime = new DeviceRuntimePort(descriptors, loggerFactory: _loggerFactory);
            _logger.LogInformation("Device runtime initialized. configuredDevices={Count}", descriptors.Length);
            return _runtime;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_runtime is not null)
            {
                await _runtime.DisposeAsync().ConfigureAwait(false);
                _runtime = null;
            }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private static DeviceDescriptor MapToDescriptor(DeviceModel model)
        => new(
            Name: model.Name,
            Vendor: model.Vendor,
            Model: model.Model,
            TransportType: model.CommType,
            TransportPort: model.CommPort,
            TransportParam: model.CommParam,
            ProtocolName: string.Empty,
            PollingMs: model.PollingMs,
            Validate: true,
            DeviceType: model.DeviceType,
            DriverType: model.DriverType,
            DeviceId: model.Id);
}
