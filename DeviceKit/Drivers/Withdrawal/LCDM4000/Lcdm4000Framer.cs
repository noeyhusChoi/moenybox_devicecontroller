using System.Buffers;

namespace DeviceKit.Drivers.LCDM4000;

internal sealed class Lcdm4000Framer : IFramer
{
    private const int MinResponseLength = 6;
    private const int MaxFrameBytes = 4096;

    public bool TryExtractFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame)
    {
        while (true)
        {
            if (buffer.Length == 0)
            {
                frame = default;
                return false;
            }

            byte first = PeekByte(buffer, 0);
            if (first == Lcdm4000Protocol.ACK || first == Lcdm4000Protocol.NAK)
            {
                frame = buffer.Slice(0, 1);
                buffer = buffer.Slice(1);
                return true;
            }

            if (first != Lcdm4000Protocol.SOH)
            {
                buffer = buffer.Slice(1);
                continue;
            }

            if (buffer.Length < MinResponseLength)
            {
                frame = default;
                return false;
            }

            int etxPos = FindEtx(buffer);
            if (etxPos < 0)
            {
                frame = default;
                return false;
            }

            int total = etxPos + 2;
            if (total <= 0 || total > MaxFrameBytes || buffer.Length < total)
            {
                frame = default;
                return false;
            }

            frame = buffer.Slice(0, total);
            buffer = buffer.Slice(total);
            return true;
        }
    }

    public byte[] MakeFrame(ReadOnlySpan<byte> payload) => payload.ToArray();

    private static int FindEtx(ReadOnlySequence<byte> buffer)
    {
        long offset = 0;
        foreach (var segment in buffer)
        {
            var span = segment.Span;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == Lcdm4000Protocol.ETX && offset + i >= 4)
                    return (int)(offset + i);
            }

            offset += span.Length;
        }

        return -1;
    }

    private static byte PeekByte(ReadOnlySequence<byte> buffer, long offset)
    {
        if (offset == 0 && buffer.FirstSpan.Length > 0)
            return buffer.FirstSpan[0];

        return buffer.Slice(offset, 1).FirstSpan[0];
    }
}
