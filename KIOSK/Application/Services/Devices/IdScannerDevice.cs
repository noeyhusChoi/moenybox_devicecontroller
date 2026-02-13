using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using Pr22.Processing;

namespace KIOSK.Application.Services.Devices
{
    public sealed class IdScannerDevice : IIdScannerDevice
    {
        private const string DeviceId = "IDSCANNER1";
        private readonly IIdScannerDeviceService _service;

        public IdScannerDevice(IIdScannerDeviceService service)
        {
            _service = service;
        }

        public event EventHandler? Detected
        {
            add => _service.Detected += value;
            remove => _service.Detected -= value;
        }

        public Task<Page?> SaveImageAsync(CancellationToken ct) =>
            _service.SaveImageAsync(DeviceId, ct);

        public Task<CommandResult> ScanStartAsync(CancellationToken ct) =>
            _service.ScanStartAsync(DeviceId, ct);

        public Task<CommandResult> GetScanStatusAsync(CancellationToken ct) =>
            _service.GetScanStatusAsync(DeviceId, ct);

        public Task ScanStopAsync(CancellationToken ct) =>
            _service.ScanStopAsync(DeviceId, ct);
    }
}
