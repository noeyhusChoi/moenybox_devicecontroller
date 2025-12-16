using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DeviceController.Core.Abstractions;
using DeviceController.Core.Commands;
using DeviceController.Core.States;

namespace DeviceController.Core.Base
{
    /// <summary>
    /// Common device skeleton implementing queueing, deduplication, and in-flight single execution.
    /// </summary>
    public abstract class DeviceBase<TCommandId> : IDevice where TCommandId : struct, Enum
    {
        private readonly IDeviceClient _client;
        private readonly IDeviceProtocol<TCommandId> _protocol;
        private readonly Channel<DeviceCommand<TCommandId>> _queue;
        private readonly HashSet<TCommandId> _queuedGuard = new();
        private readonly HashSet<TCommandId> _inFlightGuard = new();
        private readonly object _sync = new();
        private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(2);
        private readonly TimeSpan _idleDelay = TimeSpan.FromMilliseconds(250);
        private readonly CancellationTokenSource _cts = new();
        private Task? _runner;

        protected DeviceBase(string deviceId, IDeviceClient client, IDeviceProtocol<TCommandId> protocol, DeviceStateSnapshot initialState)
        {
            DeviceId = deviceId;
            _client = client;
            _protocol = protocol;
            State = initialState;
            Commands = protocol.DescribeCommands();
            _queue = Channel.CreateUnbounded<DeviceCommand<TCommandId>>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        }

        public string DeviceId { get; }
        public DeviceStateSnapshot State { get; private set; }
        public IReadOnlyList<DeviceCommandMetadata> Commands { get; }
        public event EventHandler<DeviceStateSnapshot>? StateChanged;

        public bool CanExecute(Enum commandId)
        {
            if (commandId is not TCommandId typed) return false;
            if (State.ConnectionState != ConnectionState.Connected) return false;
            if (State.HealthState == HealthState.Fault) return false;
            return IsAllowedInState(typed, State);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_runner != null) return Task.CompletedTask;
            _runner = Task.Run(() => RunAsync(_cts.Token), cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            if (_runner != null)
            {
                try
                {
                    await _runner.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            await _client.DisposeAsync().ConfigureAwait(false);
        }

        public Task<CommandResult> EnqueueAsync(IDeviceCommand command, CancellationToken cancellationToken)
        {
            if (command is not DeviceCommand<TCommandId> typed)
            {
                throw new InvalidOperationException($"Invalid command type for {DeviceId}: {command.CommandId}");
            }

            if (!CanExecute(typed.CommandId))
            {
                return Task.FromResult(CommandResult.Rejected("Command not allowed in current state."));
            }

            lock (_sync)
            {
                if (_queuedGuard.Contains(typed.CommandId) || _inFlightGuard.Contains(typed.CommandId))
                {
                    return Task.FromResult(CommandResult.Rejected("Duplicate command already queued or running."));
                }

                _queuedGuard.Add(typed.CommandId);
            }

            return WriteToQueueAsync(typed, cancellationToken);
        }

        protected virtual bool IsAllowedInState(TCommandId commandId, DeviceStateSnapshot state) => true;

        private async Task<CommandResult> WriteToQueueAsync(DeviceCommand<TCommandId> command, CancellationToken cancellationToken)
        {
            var accepted = CommandResult.Accepted($"Queued {command.CommandId}");
            await _queue.Writer.WriteAsync(command, cancellationToken).ConfigureAwait(false);
            return accepted;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

                    if (_queue.Reader.TryRead(out var command))
                    {
                        await ProcessCommandAsync(command, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    await Task.Delay(_idleDelay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Unexpected failure moves device into a faulted health state but keeps reconnecting.
                    UpdateState(State.With(health: HealthState.Fault, detail: $"Internal error: {ex.Message}"));
                    await Task.Delay(_reconnectDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (await _client.IsConnectedAsync(cancellationToken).ConfigureAwait(false))
            {
                if (State.ConnectionState != ConnectionState.Connected)
                {
                    UpdateState(State.With(connection: ConnectionState.Connected, health: State.HealthState, detail: "Link restored."));
                }

                return true;
            }

            UpdateState(State.With(connection: ConnectionState.Connecting, detail: "Connecting..."));
            var result = await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (result.Success)
            {
                UpdateState(State.With(connection: ConnectionState.Connected, health: State.HealthState, detail: "Connected"));
                return true;
            }

            UpdateState(DeviceStateSnapshot.Disconnected(result.Message ?? "Connection failed"));
            return false;
        }

        private async Task ProcessCommandAsync(DeviceCommand<TCommandId> command, CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                _queuedGuard.Remove(command.CommandId);
                _inFlightGuard.Add(command.CommandId);
            }

            var result = await ExecuteCommandSafeAsync(command, cancellationToken).ConfigureAwait(false);

            if (_protocol.IsStatusCommand(command.CommandId))
            {
                var newState = _protocol.ApplyStatus(State, command, result);
                UpdateState(newState);
            }
            else
            {
                if (result.Status == CommandStatus.Failed || result.Status == CommandStatus.Timeout)
                {
                    UpdateState(State.With(health: HealthState.Degraded, detail: result.Message ?? "Command failed."));
                }
            }

            if (result.Status == CommandStatus.Failed)
            {
                var connected = await _client.IsConnectedAsync(cancellationToken).ConfigureAwait(false);
                if (!connected)
                {
                    UpdateState(DeviceStateSnapshot.Disconnected(result.Message ?? "Link lost."));
                }
            }

            lock (_sync)
            {
                _inFlightGuard.Remove(command.CommandId);
            }
        }

        private async Task<CommandResult> ExecuteCommandSafeAsync(DeviceCommand<TCommandId> command, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _protocol.ExecuteAsync(command, _client, cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException)
            {
                return CommandResult.Timeout($"Command {command.CommandId} canceled.");
            }
            catch (Exception ex)
            {
                // Protocol errors are surfaced as CommandResult rather than exceptions.
                return CommandResult.Failed($"Protocol error: {ex.Message}");
            }
        }

        protected void UpdateState(DeviceStateSnapshot newState)
        {
            if (newState == State) return;
            State = newState with { Timestamp = DateTimeOffset.UtcNow };
            StateChanged?.Invoke(this, State);
        }
    }
}
