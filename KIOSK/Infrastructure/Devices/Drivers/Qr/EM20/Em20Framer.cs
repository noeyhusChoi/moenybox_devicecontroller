using System;
using System.Buffers;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;

namespace KIOSK.Device.Drivers.EM20;

/// <summary>
/// EM20 is line-based; use passthrough framing for now.
/// </summary>
public sealed class Em20Framer : IFramer
{
    public bool TryExtractFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame)
        => PassthroughFramer.Instance.TryExtractFrame(ref buffer, out frame);

    public byte[] MakeFrame(ReadOnlySpan<byte> payload)
        => PassthroughFramer.Instance.MakeFrame(payload);
}
