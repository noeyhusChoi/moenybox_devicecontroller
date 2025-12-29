using System;
using System.Buffers;
using System.Text;
using KIOSK.Device.Abstractions;

namespace KIOSK.Devices.Drivers.HCDM20K;

/// <summary>
/// HCDM-20K 프레이머: ACK/NAK/ENQ(1바이트) 또는 STX ... ETX CRC 프레임 추출.
/// </summary>
internal sealed class Hcdm20kFramer : IFramer
{
    private const byte STX = 0x02;
    private const byte ETX = 0x03;
    private const byte ENQ = 0x05;
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
            if (first == ACK || first == NAK || first == ENQ)
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

            if (buffer.Length < 6)
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

            int total = etxPos + 1 + 2;
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

    public byte[] MakeFrame(ReadOnlySpan<byte> payload)
        => payload.ToArray();

    public static byte[] BuildFrame(Hcdm20kCommand cmd, byte[] data)
    {
        int len = 1 + data.Length + 1 + 2;
        var buf = new byte[1 + len];
        int idx = 0;
        buf[idx++] = STX;
        buf[idx++] = (byte)cmd;
        if (data.Length > 0)
        {
            Buffer.BlockCopy(data, 0, buf, idx, data.Length);
            idx += data.Length;
        }
        buf[idx++] = ETX;

        ushort crc = Crc16IbM(buf, 0, idx);
        buf[idx++] = (byte)((crc >> 8) & 0xFF);
        buf[idx++] = (byte)(crc & 0xFF);
        return buf;
    }

    public static (bool ok, string err, byte[] data)? ParseResponse(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 1 + 1 + 2 + 1 + 2) return null;
        if (frame[0] != STX) return null;

        int etxPos = frame.Length - 3;
        if (etxPos < 3) return null;
        if (frame[etxPos] != ETX) return null;

        ushort expectedCrc = (ushort)((frame[^2] << 8) | frame[^1]);
        ushort calc = Crc16IbM(frame.ToArray(), 0, frame.Length - 2);
        if (expectedCrc != calc) return null;

        string err = Encoding.ASCII.GetString(frame.Slice(2, 2));
        var data = frame.Slice(4, etxPos - 4).ToArray();
        bool ok = string.Equals(err, "00", StringComparison.OrdinalIgnoreCase);
        return (ok, err, data);
    }

    private static ushort Crc16IbM(byte[] data, int offset, int count)
    {
        ushort crc = 0xFFFF;
        for (int i = 0; i < count; i++)
        {
            crc ^= (ushort)(data[offset + i] << 8);
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc <<= 1;
            }
        }
        return crc;
    }

    private static int FindEtx(ReadOnlySequence<byte> buffer)
    {
        long index = 0;
        foreach (var segment in buffer)
        {
            var span = segment.Span;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == ETX && index + i >= 3)
                    return (int)(index + i);
            }
            index += span.Length;
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
