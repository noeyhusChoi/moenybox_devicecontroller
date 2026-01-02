// Abstractions/IFramer.cs
using System;
using System.Buffers;

namespace KIOSK.Device.Abstractions
{
    /// <summary>
    /// 수신된 바이트에서 "한 개의 프레임"을 잘라내는 역할
    /// </summary>
    public interface IFramer
    {
        /// <summary>완전한 프레임을 찾으면 frame = 그 바이트 범위, 그렇지 않으면 false</summary>
        bool TryExtractFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame);
        /// <summary>송신 시 바이트 프레임 만들기(헤더/길이/CRC 등 포함)</summary>
        byte[] MakeFrame(ReadOnlySpan<byte> payload);
    }
}
