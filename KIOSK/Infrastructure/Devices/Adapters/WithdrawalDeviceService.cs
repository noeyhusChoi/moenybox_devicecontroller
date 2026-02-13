using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Adapters
{
    public sealed class WithdrawalDeviceService : IWithdrawalDeviceService
    {
        private readonly IDeviceManager _deviceManager;

        public WithdrawalDeviceService(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public async Task<Dictionary<int, (int req, int exit, int rej)>?> DispenseAsync(
            string deviceId,
            byte[] payload,
            CancellationToken ct)
        {
            var response = await _deviceManager.SendAsync(deviceId, new DeviceCommand("Dispense", payload));
            if (!response.Success)
            {
                Trace.WriteLine($"Dispense Command Failed for Device {deviceId}, {response.Message}");
                return null;
            }

            if (response.Data is not byte[] rawBytes)
                return null;

            return ParseDispenseResponse(rawBytes);
        }

        private static Dictionary<int, (int req, int exit, int rej)>? ParseDispenseResponse(byte[] data)
        {
            ReadOnlySpan<byte> payload = data[5..];

            const int chunkSize = 13;
            if (payload.Length % chunkSize != 0)
            {
                Trace.WriteLine("Dispense Response Failed");
                return null;
            }

            int groupCount = payload.Length / chunkSize;
            var chunks = new List<byte[]>(groupCount);

            int offset = 0;
            for (int i = 0; i < groupCount; i++)
            {
                var slice = payload.Slice(offset, chunkSize);
                chunks.Add(slice.ToArray());
                offset += chunkSize;
            }

            var map = new Dictionary<int, (int, int, int)>();
            foreach (var chunk in chunks)
            {
                var req = chunk[0];
                var exit = chunk[4];
                var rej = chunk[5];
                map.Add(map.Count, (req, exit, rej));
            }

            return map;
        }
    }
}
