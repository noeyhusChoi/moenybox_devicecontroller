namespace DeviceKit.Events; 
 
internal sealed record DeviceDriverEvent(string EventName, object? Payload = null, int Version = 1); 
