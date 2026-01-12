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
    Shutter = 2,
    Status = 3,
    Gate = 4,
    Cassette1 = 5,
    Cassette2 = 6,
    Cassette3 = 7,
    Cassette4 = 8
}

[Flags]
internal enum Hcdm10kGateFlags : byte
{
    Exit1Detected = 1 << 0,
    RejectInDetected = 1 << 1,
    Gate1Detected = 1 << 2,
    Gate2Detected = 1 << 3,
    ScanStart = 1 << 4
}

internal static class Hcdm10kShutterBits
{
    public const byte ShutIn1 = 1 << 0;
    public const byte ShutIn2 = 1 << 1;
    public const byte ShutIn3 = 1 << 2;
    public const byte ShutClose = 1 << 3;
    public const byte ShutOpen = 1 << 4;
}

internal static class Hcdm10kStatusBits
{
    public const byte RejectBoxOpen = 1 << 0;
    public const byte CisOpen = 1 << 1;
    public const byte Msol = 1 << 2;
}

internal static class Hcdm10kCassetteBits
{
    public const byte Skew1 = 1 << 0;
    public const byte Skew2 = 1 << 1;
    public const byte NearEnd = 1 << 2;
    public const byte Mount = 1 << 3;
    public const byte Id1A = 1 << 4;
    public const byte Id2A = 1 << 5;
}
