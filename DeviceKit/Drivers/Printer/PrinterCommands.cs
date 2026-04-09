namespace DeviceKit.Drivers.Printer;

public static class PrinterCommands
{
    public const string PrintTitleName = "PRINTTITLE";
    public const string PrintContentName = "PRINTCONTENT";
    public const string CutName = "CUT";

    public static DeviceCommandRequest PrintTitle(string content) => new(PrintTitleName, content);
    public static DeviceCommandRequest PrintContent(string content) => new(PrintContentName, content);
    public static DeviceCommandRequest Cut() => new(CutName);
}
