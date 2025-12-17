using System;
using System.Collections.Generic;

namespace KIOSK.Device.Drivers.E200Z;

/// <summary>
/// SSI Opcode 정의 (필요한 것만).
/// </summary>
public enum SsiOpcode : byte
{
    CMD_ACK = 0xD0,
    CMD_NAK = 0xD1,
    DECODE_DATA = 0xF3,     // 1D
    DECODE_DATA_TWO = 0xF4, // 2D (QR 등, Extended 가능)
    LED_ON = 0xE7,
    LED_OFF = 0xE8,
    SCAN_ENABLE = 0xE9,
    SCAN_DISABLE = 0xEA,
    SLEEP = 0xEB,
    START_DECODE = 0xE4,
    STOP_DECODE = 0xE5,
    REQUEST_REVISION = 0xA3,
    REPLY_REVISION = 0xA4,
    RESET = 0xFA,
    CFG_PARAM_SEND = 0xC6 // 설정값 변경
}

/// <summary>
/// SSI 기본 패킷 (STD 형식).
/// Length <= 255 (Extended 아닌 경우).
/// </summary>
public sealed class SsiPacket
{
    public byte Length { get; set; }  // 체크섬 제외 전체 길이
    public byte Opcode { get; set; }
    public byte Source { get; set; }  // 0x04 = Host, 0x00 = Engine
    public byte Status { get; set; }  // Bit3: Change Type (영구 저장), Bit0: Retransmit
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public ushort Checksum { get; set; }

    public byte[] ToBytes()
    {
        var list = new List<byte>(Length + 2);
        list.Add(Length);
        list.Add(Opcode);
        list.Add(Source);
        list.Add(Status);

        if (Data is { Length: > 0 })
            list.AddRange(Data);

        ushort checksum = ComputeChecksum(list.ToArray(), 0, list.Count);
        Checksum = checksum;

        list.Add((byte)(checksum >> 8));   // High
        list.Add((byte)(checksum & 0xFF)); // Low

        return list.ToArray();
    }

    public static SsiPacket CreateSimple(SsiOpcode opcode)
    {
        return new SsiPacket
        {
            Length = 0x04,
            Opcode = (byte)opcode,
            Source = 0x04,
            Status = 0x00,
            Data = Array.Empty<byte>()
        };
    }

    /// <summary>
    /// 파라미터 설정용 패킷 (Group=0x00, Param=param, Value=value).
    /// Status bit3 = 1이면 플래시에 저장(영구).
    /// </summary>
    public static SsiPacket CreateParamByte(byte param, byte value, bool saveToFlash = true)
    {
        byte status = saveToFlash ? (byte)0x08 : (byte)0x00;
        byte[] data = new byte[] { 0x00, param, value }; // [Group=0x00, Param, Value]

        return new SsiPacket
        {
            Length = (byte)(4 + data.Length),
            Opcode = (byte)SsiOpcode.CFG_PARAM_SEND,
            Source = 0x04,
            Status = status,
            Data = data
        };
    }

    /// <summary>
    /// buffer[offset..offset+count-1] 합에 대한 16비트 2's complement.
    /// </summary>
    public static ushort ComputeChecksum(byte[] buffer, int offset, int count)
    {
        uint sum = 0;
        for (int i = 0; i < count; i++)
            sum += buffer[offset + i];

        return (ushort)(0 - sum);
    }
}

/// <summary>
/// 디코드된 바코드 메시지.
/// </summary>
public sealed class DecodeMessage
{
    public bool IsExtended { get; set; }
    public byte BarcodeType { get; set; }  // QR: 0xF1 (매뉴얼 기준)
    public string Text { get; set; } = "";
    public byte[] RawData { get; set; } = Array.Empty<byte>();
}

internal readonly record struct SsiParsed(SsiOpcode Opcode, byte Source, byte Status, byte[] Data, bool Extended);

