using System;

namespace DeviceController.Core.Commands
{
    public record CommandResult(CommandStatus Status, string? Message = null, object? Data = null, DateTimeOffset? Timestamp = null)
    {
        public static CommandResult Accepted(string? message = null) => new(CommandStatus.Accepted, message, null, DateTimeOffset.UtcNow);

        public static CommandResult Completed(string? message = null, object? data = null) => new(CommandStatus.Completed, message, data, DateTimeOffset.UtcNow);

        public static CommandResult Failed(string? message = null, object? data = null) => new(CommandStatus.Failed, message, data, DateTimeOffset.UtcNow);

        public static CommandResult Rejected(string? message = null) => new(CommandStatus.Rejected, message, null, DateTimeOffset.UtcNow);

        public static CommandResult Timeout(string? message = null) => new(CommandStatus.Timeout, message, null, DateTimeOffset.UtcNow);
    }
}
