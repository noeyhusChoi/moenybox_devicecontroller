namespace DeviceKit.Events;

public sealed record DeviceEventEnvelope(
    string DeviceId,
    string DeviceType,
    string EventName,
    DateTimeOffset OccurredAt,
    string PayloadJson,
    int Version = 1);
