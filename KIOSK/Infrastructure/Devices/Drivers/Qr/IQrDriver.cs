using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers;

public sealed record QrDecodedData(byte BarcodeType, string Text);

public interface IQrDriver : IDeviceDriver
{
    event EventHandler<QrDecodedData>? Decoded;

    Task<CommandResult> EnableScanAsync(CancellationToken ct = default);
    Task<CommandResult> DisableScanAsync(CancellationToken ct = default);
}
