using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Abstractions;
using DeviceController.Core.Commands;

namespace DeviceController.Services
{
    /// <summary>
    /// Periodically enqueues status commands per device, relying on queue de-duplication and CanExecute for safety.
    /// </summary>
    public class StatusPollingService : IAsyncDisposable
    {
        private readonly IDeviceRegistry _registry;
        private readonly TimeSpan _interval;
        private readonly List<Task> _loops = new();
        private CancellationTokenSource? _cts;

        public StatusPollingService(IDeviceRegistry registry, TimeSpan interval)
        {
            _registry = registry;
            _interval = interval;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_cts != null) return Task.CompletedTask;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            foreach (var device in _registry.Devices)
            {
                var loop = Task.Run(() => PollDeviceAsync(device, _cts.Token), _cts.Token);
                _loops.Add(loop);
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_cts == null) return;
            _cts.Cancel();
            try
            {
                await Task.WhenAll(_loops).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            _cts.Dispose();
            _cts = null;
            _loops.Clear();
        }

        private async Task PollDeviceAsync(IDevice device, CancellationToken cancellationToken)
        {
            var statusMeta = device.Commands.FirstOrDefault(c => c.IsStatusCommand);
            if (statusMeta == null)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var command = CreateCommand(device, statusMeta);
                    await device.EnqueueAsync(command, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Swallow to keep polling alive; device logic handles state transitions.
                }

                try
                {
                    await Task.Delay(_interval, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static IDeviceCommand CreateCommand(IDevice device, DeviceCommandMetadata metadata)
        {
            var enumType = metadata.CommandId.GetType();
            var deviceCommandType = typeof(DeviceCommand<>).MakeGenericType(enumType);
            var instance = Activator.CreateInstance(deviceCommandType, device.DeviceId, metadata.CommandId, null);
            if (instance is IDeviceCommand command)
            {
                return command;
            }

            throw new InvalidOperationException($"Unable to create status command for {metadata.CommandId}");
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
        }
    }
}
