using System;

namespace KIOSK.Devices.Drivers.HCDM;

internal static class Hcdm10kProtocol
{
    public const byte STX = 0x02;
    public const byte ETX = 0x03;
    public const byte ENQ = 0x05;
    public const byte ACK = 0x06;
    public const byte NAK = 0x15;

    public const int AckWaitMs = 500;
    public const int MaxEnq = 10;
    public const int MaxNak = 3;
}

internal enum Hcdm10kCommand : byte
{
    Initialize = (byte)'I',
    Sensor = (byte)'S',
    Dispense = (byte)'D',
    Eject = (byte)'F'
}

internal enum Hcdm10kSensorIndex
{
    Door = 3,
    Gate = 4,
    Cassette1 = 5,
    Cassette2 = 6,
    Cassette3 = 7,
    Cassette4 = 8
}

[Flags]
internal enum Hcdm10kGateFlags : byte
{
    EjectDetected = 1 << 0,
    CollectDetected = 1 << 1,
    Gate1Detected = 1 << 2,
    Gate2Detected = 1 << 3
}

internal static class Hcdm10kDoorBits
{
    public const byte RejectBoxOpen = 1 << 0;
}

internal static class Hcdm10kCassetteBits
{
    public const byte Skew1 = 1 << 0;
    public const byte Present = 1 << 1;
    public const byte LowLevel = 1 << 2;   // 0이면 부족/오류로 해석
    public const byte Mounted = 1 << 3;    // 0이면 미장착
    public const byte DipSwitch1 = 1 << 4;
    public const byte DipSwitch2 = 1 << 5;
}
