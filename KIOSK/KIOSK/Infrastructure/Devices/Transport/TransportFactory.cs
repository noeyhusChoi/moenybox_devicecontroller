// Core/TransportFactory.cs
using KIOSK.Device.Abstractions;
using System.IO.Ports;

namespace KIOSK.Device.Transport;

public static class TransportFactory
{
    public static ITransport Create(DeviceDescriptor d)
    {
        switch (d.TransportType?.ToUpperInvariant())
        {
            case "SERIAL":
                {
                    var port = d.TransportPort;
                    var (baud, databits, stopbits, parity) = ParseSerial(d.TransportParam);
                    return new TransportSerial(port, baud, databits, stopbits, parity);
                }
            case "TCP":
                {
                    var host = d.TransportPort;
                    var port = int.TryParse(d.TransportParam, out var p) ? p : 502;
                    return new TransportTcp(host, port);
                }
            case "MODBUS_RTU":
                {
                    var port = d.TransportPort;
                    var (baud, databits, stopbits, parity) = ParseSerial(d.TransportParam);
                    return new TransportModbusRtu(port, baud, databits, stopbits, parity);
                }
            case "PR22":
                {
                    return new TransportPr22();
                }
            case "NONE":
                {
                    return new TransportNone();
                }
            default:
                throw new NotSupportedException($"Unknown transport: {d.TransportType}");
        }
    }

    private static (int baudRate, int dataBits, StopBits stopBits, Parity parity) ParseSerial(string s)
    {
        var sp = s.Split(',', StringSplitOptions.RemoveEmptyEntries);

        int baudRate = (sp.Length > 0 && int.TryParse(sp[0], out var b)) ? b : 9600;
        int dataBits = (sp.Length > 1 && int.TryParse(sp[1], out var d)) ? d : 8;
        StopBits stopBits = (sp.Length > 2 && Enum.TryParse(sp[2], true, out StopBits sb)) ? sb : StopBits.One;
        Parity parity = (sp.Length > 3 && Enum.TryParse(sp[3], true, out Parity p)) ? p : Parity.None;

        return (baudRate, dataBits, stopBits, parity);
    }
}
