using System;
using System.Buffers;
using DeviceKit.Transport;

namespace DeviceKit.Drivers.EM20;

/// <summary>
/// EM20 is line-based. Extract frames on CR/LF boundaries.
/// </summary>
internal sealed class Em20Framer : IFramer
{
    public bool TryExtractFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame)
    {
        if (buffer.Length == 0)
        {
            frame = default;
            return false;
        }

        var end = buffer.PositionOf((byte)'\n');
        end ??= buffer.PositionOf((byte)'\r');
        if (end is null)
        {
            frame = default;
            return false;
        }

        frame = buffer.Slice(0, end.Value);
        var next = buffer.GetPosition(1, end.Value);
        while (buffer.Slice(next).Length > 0)
        {
            var value = buffer.Slice(next, 1).FirstSpan[0];
            if (value is not ((byte)'\r' or (byte)'\n'))
                break;

            next = buffer.GetPosition(1, next);
        }

        buffer = buffer.Slice(next);
        return true;
    }

    public byte[] MakeFrame(ReadOnlySpan<byte> payload)
        => payload.ToArray();
}
