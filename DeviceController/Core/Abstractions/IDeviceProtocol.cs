using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Commands;
using DeviceController.Core.States;

namespace DeviceController.Core.Abstractions
{
    public interface IDeviceProtocol<TCommandId> where TCommandId : struct, Enum
    {
        IReadOnlyList<DeviceCommandMetadata> DescribeCommands();
        Task<CommandResult> ExecuteAsync(DeviceCommand<TCommandId> command, IDeviceClient client, CancellationToken cancellationToken);
        DeviceStateSnapshot ApplyStatus(DeviceStateSnapshot state, DeviceCommand<TCommandId> command, CommandResult result);
        bool IsStatusCommand(TCommandId commandId);
    }
}
