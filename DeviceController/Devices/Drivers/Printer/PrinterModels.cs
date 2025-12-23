using System;

namespace KIOSK.Device.Drivers.Printer;

[Flags]
internal enum PrinterStatusFlags : byte
{
    PaperOut = 0x01,
    HeadUp = 0x02,
    PaperError = 0x04,
    PaperNearEnd = 0x08,
    Printing = 0x10,
    CutterError = 0x20,
    AuxPaperPresent = 0x80
}

internal enum PrinterAlignment : byte
{
    Left = 0,
    Center = 1,
    Right = 2
}

internal static class PrinterDefaults
{
    public const double LeftMarginMm = 3.5;
    public const int StatusTimeoutMs = 2000;
}
