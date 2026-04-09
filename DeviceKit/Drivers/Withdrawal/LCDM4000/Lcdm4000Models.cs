using System.Collections.ObjectModel;

namespace DeviceKit.Drivers.LCDM4000;

internal static class Lcdm4000Protocol
{
    public const byte SOH = 0x01;
    public const byte STX = 0x02;
    public const byte ETX = 0x03;
    public const byte EOT = 0x04;
    public const byte ACK = 0x06;
    public const byte NAK = 0x15;
    public const byte DeviceId = 0x30;
    public const byte NoError = 0x20;

    public const int AckWaitMs = 550;
    public const int MaxRetry = 3;
    public const int ResetDelayMs = 2000;

    public static IReadOnlyDictionary<byte, string> ErrorDescriptions { get; } =
        new ReadOnlyDictionary<byte, string>(new Dictionary<byte, string>
        {
            [0x01] = "Bill pick up error.",
            [0x02] = "Jam on the path between CHK sensor and DVT sensor.",
            [0x03] = "Jam on the path between DVT sensor and EJT sensor.",
            [0x04] = "Jam on the path between EJT sensor and EXIT sensor.",
            [0x05] = "A note is staying in EXIT sensor.",
            [0x06] = "A rejected note was ejected.",
            [0x07] = "Unexpected note count mismatch on eject sensor.",
            [0x08] = "A note that should be rejected passed the eject sensor.",
            [0x09] = "Media length on eject sensor is too long.",
            [0x0A] = "Media length on exit sensor is too long.",
            [0x0B] = "Notes detected on the path before pick-up started.",
            [0x0C] = "Too many notes dispensed in one transaction.",
            [0x0D] = "Too many reject events in one transaction.",
            [0x0E] = "Abnormal termination during purge operation.",
            [0x20] = "Sensor trouble or abnormal material detected before start.",
            [0x21] = "Sensor trouble or abnormal material detected before start.",
            [0x22] = "Solenoid operation trouble detected before dispense.",
            [0x23] = "Motor or slit sensor trouble detected before dispense.",
            [0x24] = "No cassette was requested for dispensing.",
            [0x25] = "Requested cassette is in near-end state.",
            [0x26] = "Reject tray is not present.",
            [0x29] = "Dispensed count exceeded the requested count.",
            [0x30] = "Abnormal command was recognized.",
            [0x31] = "Abnormal command parameter was recognized.",
            [0x32] = "VERIFY command is not allowed after downloading and reset.",
            [0x33] = "Program area writing failed.",
            [0x34] = "Verify failed.",
            [0x35] = "EEPROM write failed.",
            [0x36] = "EEPROM checksum error occurred while writing.",
            [0x40] = "Top cassette note was detected while dispensing from another cassette.",
            [0x41] = "Second cassette note was detected while dispensing from another cassette.",
            [0x42] = "Third cassette note was detected while dispensing from another cassette.",
            [0x43] = "Fourth cassette note was detected while dispensing from another cassette."
        });

    public static bool IsSuccess(byte errorByte) => errorByte == NoError;

    public static byte EncodeCount(int count)
    {
        if (count is < 0 or > 100)
            throw new InvalidOperationException($"LCDM4000 count must be between 0 and 100. count={count}");

        return checked((byte)(count + 0x20));
    }

    public static int DecodeCount(byte value) => Math.Max(0, value - 0x20);

    public static byte DecodeError(byte errorByte)
        => errorByte >= 0x20 ? (byte)(errorByte - 0x20) : errorByte;

    public static string GetErrorDetail(byte errorByte)
    {
        var raw = DecodeError(errorByte);
        return raw == 0 ? "OK" : $"ERR_{raw:X2}";
    }

    public static string? DescribeError(byte errorByte)
    {
        var raw = DecodeError(errorByte);
        return raw == 0
            ? null
            : ErrorDescriptions.TryGetValue(raw, out var description)
                ? description
                : $"LCDM4000 returned error 0x{raw:X2}.";
    }
}

internal enum Lcdm4000Command : byte
{
    Reset = 0x44,
    Status = 0x50,
    Purge = 0x51,
    Dispense = 0x52,
    TestDispense = 0x53,
    LastStatus = 0x55,
    SensorDiagnostics = 0x58,
    Supplementary = 0x71
}

internal enum Lcdm4000SupplementaryCommand : byte
{
    Version = 0x30
}

internal sealed record Lcdm4000Response(
    byte ResponseCode,
    byte ErrorByte,
    byte[] Data)
{
    public bool Success => Lcdm4000Protocol.IsSuccess(ErrorByte);
    public string? ErrorMessage => Lcdm4000Protocol.DescribeError(ErrorByte);
}
