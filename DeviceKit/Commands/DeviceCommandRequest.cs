namespace DeviceKit.Commands;

public sealed record DeviceCommandRequest(
    string Name, 
    object? Payload = null);
