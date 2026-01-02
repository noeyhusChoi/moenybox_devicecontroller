using System;
using System.Buffers;
using KIOSK.Device.Abstractions;

namespace KIOSK.Devices.Drivers.HCDM;

/// <summary>
/// HCDM-10K 프레이머: ACK/NAK(1바이트) 또는 STX LENL LENH ... ETX CHK 프레임 추출.
/// </summary>
internal sealed class Hcdm10kFramer : IFramer
{
    private const byte STX = 0x02;
    private const byte ETX = 0x03;
    private const byte ACK = 0x06;
    private const byte NAK = 0x15;

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

            // 단일 제어 바이트(ACK/NAK)
            if (first == ACK || first == NAK)
            {
                frame = buffer.Slice(0, 1);
                buffer = buffer.Slice(1);
                return true;
            }

            if (first != STX)
            {
                buffer = buffer.Slice(1);
                continue;
            }

            if (buffer.Length < 6) // STX LENL LENH CMD ETX CHK 최소
            {
                frame = default;
                return false;
            }

            byte lenL = PeekByte(buffer, 1);
            byte lenH = PeekByte(buffer, 2);
            int len = lenL | (lenH << 8);

            if (len <= 0 || len > MaxFrameBytes)
            {
                buffer = buffer.Slice(1);
                continue;
            }

            int total = 1 + 2 + len + 1 + 1; // STX + LEN(2) + payload(len) + ETX + CHK
            if (buffer.Length < total)
            {
                frame = default;
                return false;
            }

            frame = buffer.Slice(0, total);
            buffer = buffer.Slice(total);
            return true;
        }
    }

    public byte[] MakeFrame(ReadOnlySpan<byte> payload)
        => payload.ToArray();

    private static byte PeekByte(ReadOnlySequence<byte> buffer, long offset)
    {
        if (offset == 0 && buffer.FirstSpan.Length > 0)
            return buffer.FirstSpan[0];

        return buffer.Slice(offset, 1).FirstSpan[0];
    }
}

