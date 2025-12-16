using System;
using DeviceController.Devices.Scanner;

namespace DeviceController.Services
{
    public interface IDecodeEventBus
    {
        event EventHandler<ScannerDecodeData>? DecodeReceived;
        void Publish(ScannerDecodeData data);
    }

    public class DecodeEventBus : IDecodeEventBus
    {
        public event EventHandler<ScannerDecodeData>? DecodeReceived;

        public void Publish(ScannerDecodeData data)
        {
            DecodeReceived?.Invoke(this, data);
        }
    }
}
