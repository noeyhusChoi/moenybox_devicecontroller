using System;
using System.IO.Ports;

namespace DeviceKit.Transport;

internal static class StreamPortFactory
{
    public static ITransport Create(DeviceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        switch ((descriptor.TransportType ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "SERIAL":
            {
                var (baud, dataBits, stopBits, parity) = ParseSerial(descriptor.TransportParam);
                return new TransportSerial(descriptor.TransportPort, baud, dataBits, stopBits, parity);
            }
            case "TCP":
            {
                var port = int.TryParse(descriptor.TransportParam, out var parsed) ? parsed : 502;
                return new TransportTcp(descriptor.TransportPort, port);
            }
            case "MODBUS_RTU":
            {
                var (baud, dataBits, stopBits, parity) = ParseSerial(descriptor.TransportParam);
                return new TransportModbusRtu(descriptor.TransportPort, baud, dataBits, stopBits, parity);
            }
            default:
                throw new NotSupportedException($"Unsupported stream transport: {descriptor.TransportType}");
        }
    }

    private static (int baudRate, int dataBits, StopBits stopBits, Parity parity) ParseSerial(string? value)
    {
        var parts = (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);

        var baudRate = parts.Length > 0 && int.TryParse(parts[0], out var baud) ? baud : 9600;
        var dataBits = parts.Length > 1 && int.TryParse(parts[1], out var bits) ? bits : 8;
        var stopBits = parts.Length > 2 && Enum.TryParse(parts[2], true, out StopBits parsedStopBits)
            ? parsedStopBits
            : StopBits.One;
        var parity = parts.Length > 3 && Enum.TryParse(parts[3], true, out Parity parsedParity)
            ? parsedParity
            : Parity.None;

        return (baudRate, dataBits, stopBits, parity);
    }
}
