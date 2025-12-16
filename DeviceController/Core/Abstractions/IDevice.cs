using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Commands;
using DeviceController.Core.States;

namespace DeviceController.Core.Abstractions
{
    public interface IDevice
    {
        string DeviceId { get; }
        DeviceStateSnapshot State { get; }
        IReadOnlyList<DeviceCommandMetadata> Commands { get; }
        event EventHandler<DeviceStateSnapshot>? StateChanged;

        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        bool CanExecute(Enum commandId);
        Task<CommandResult> EnqueueAsync(IDeviceCommand command, CancellationToken cancellationToken);
    }

    public interface IDeviceRegistry
    {
        IReadOnlyList<IDevice> Devices { get; }
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
