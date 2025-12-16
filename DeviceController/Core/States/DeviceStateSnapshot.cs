using System;

namespace DeviceController.Core.States
{
    public record DeviceStateSnapshot(
        ConnectionState ConnectionState,
        HealthState HealthState,
        string? Detail = null,
        DateTimeOffset? Timestamp = null)
    {
        public static DeviceStateSnapshot Disconnected(string? detail = null) =>
            new(ConnectionState.Disconnected, HealthState.Unknown, detail, DateTimeOffset.UtcNow);

        public static DeviceStateSnapshot Connected(string? detail = null, HealthState? health = null) =>
            new(ConnectionState.Connected, health ?? HealthState.Ready, detail, DateTimeOffset.UtcNow);

        public DeviceStateSnapshot With(ConnectionState? connection = null, HealthState? health = null, string? detail = null) =>
            new(connection ?? ConnectionState, health ?? HealthState, detail ?? Detail, DateTimeOffset.UtcNow);
    }
}
