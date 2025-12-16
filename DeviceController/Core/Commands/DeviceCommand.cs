using System;

namespace DeviceController.Core.Commands
{
    public interface IDeviceCommand
    {
        string DeviceId { get; }
        Enum CommandId { get; }
        object? Parameter { get; }
    }

    public record DeviceCommand<TCommandId>(string DeviceId, TCommandId CommandId, object? Parameter = null) : IDeviceCommand
        where TCommandId : struct, Enum
    {
        Enum IDeviceCommand.CommandId => CommandId;
    }
}
