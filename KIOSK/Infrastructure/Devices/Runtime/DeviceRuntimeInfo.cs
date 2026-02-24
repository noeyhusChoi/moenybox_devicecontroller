namespace KIOSK.Infrastructure.Devices.Runtime;

public sealed record DeviceRuntimeInfo(
    string DeviceId,
    string Name,
    string Vendor,
    string Model,
    string TransportType,
    string TransportPort,
    string TransportParam,
    string ProtocolName,
    int PollingMs,
    string DeviceType,
    string Driver);
