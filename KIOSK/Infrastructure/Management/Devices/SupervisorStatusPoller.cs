using KIOSK.Device.Abstractions;

namespace KIOSK.Infrastructure.Management.Devices
{
    internal sealed class SupervisorStatusPoller
    {
        private readonly DeviceDescriptor _desc;

        public SupervisorStatusPoller(DeviceDescriptor desc)
        {
            _desc = desc ?? throw new ArgumentNullException(nameof(desc));
        }

        public async Task RunAsync(
            IDevice device,
            SemaphoreSlim gate,
            CancellationToken ct,
            Action<StatusSnapshot> onSnapshot)
        {
            var pollMs = Math.Max(1000, _desc.PollingMs);

            while (!ct.IsCancellationRequested)
            {
                await gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    var snapshot = await device.GetStatusAsync(ct).ConfigureAwait(false);
                    onSnapshot(snapshot);
                }
                finally
                {
                    gate.Release();
                }

                await Task.Delay(pollMs, ct).ConfigureAwait(false);
            }
        }
    }
}
