using System;
using System.Buffers;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers.E200Z;

/// <summary>
/// E200Z SSI 프레이머: 바이트 스트림에서 "한 패킷" 단위로 잘라낸다.
/// - 체크섬이 맞지 않으면 1바이트씩 버리며 재동기화 시도
/// </summary>
public sealed class E200ZFramer : IFramer
{
    private const int MaxFrameBytes = 8192;

    public bool TryExtractFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame)
    {
        while (true)
        {
            if (buffer.Length == 0)
            {
                frame = default;
                return false;
            }

            // 최소 길이 확인(표준: Len+2(checksum), 최소 payload Len=4 => total 6)
            if (buffer.Length < 6)
            {
                frame = default;
                return false;
            }

            byte first = PeekByte(buffer, 0);

            if (first != 0xFF)
            {
                int len = first;
                if (len < 4 || len > 255)
                {
                    buffer = buffer.Slice(1);
                    continue;
                }

                int total = len + 2;
                if (total > MaxFrameBytes)
                {
                    buffer = buffer.Slice(1);
                    continue;
                }

                if (buffer.Length < total)
                {
                    frame = default;
                    return false;
                }

                var candidate = buffer.Slice(0, total);
                var bytes = candidate.ToArray();

                ushort expected = SsiPacket.ComputeChecksum(bytes, 0, len);
                ushort recv = (ushort)((bytes[total - 2] << 8) | bytes[total - 1]);
                if (expected != recv)
                {
                    buffer = buffer.Slice(1);
                    continue;
                }

                frame = candidate;
                buffer = buffer.Slice(total);
                return true;
            }

            // Extended 패킷
            if (buffer.Length < 7)
            {
                frame = default;
                return false;
            }

            // length2는 [2],[3]을 사용 (기존 구현 호환)
            ushort length2 = (ushort)((PeekByte(buffer, 2) << 8) | PeekByte(buffer, 3));
            if (length2 < 7)
            {
                buffer = buffer.Slice(1);
                continue;
            }

            int totalLen = length2 + 2;
            if (totalLen > MaxFrameBytes)
            {
                buffer = buffer.Slice(1);
                continue;
            }

            if (buffer.Length < totalLen)
            {
                frame = default;
                return false;
            }

            var candidateExt = buffer.Slice(0, totalLen);
            var extBytes = candidateExt.ToArray();

            ushort expectedExt = SsiPacket.ComputeChecksum(extBytes, 0, length2);
            ushort recvExt = (ushort)((extBytes[length2] << 8) | extBytes[length2 + 1]);
            if (expectedExt != recvExt)
            {
                buffer = buffer.Slice(1);
                continue;
            }

            frame = candidateExt;
            buffer = buffer.Slice(totalLen);
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

