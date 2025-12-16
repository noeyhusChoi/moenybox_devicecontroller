using System;

namespace DeviceController.Core.Commands
{
    public record DeviceCommandMetadata(
        Enum CommandId,
        string DisplayName,
        string Description,
        bool IsStatusCommand,
        Type? ParameterType = null);
}
