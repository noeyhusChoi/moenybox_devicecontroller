using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceController.Core.Abstractions;
using DeviceController.Core.Commands;
using DeviceController.Core.States;

namespace DeviceController.ViewModels
{
    public class DeviceViewModel : ObservableObject
    {
        private readonly IDevice _device;
        private readonly Dispatcher _dispatcher;
        private ConnectionState _connectionState;
        private HealthState _healthState;
        private string? _detail;

        public DeviceViewModel(IDevice device)
        {
            _device = device;
            _dispatcher = Application.Current.Dispatcher;
            DeviceId = device.DeviceId;
            Commands = new ObservableCollection<CommandItemViewModel>();

            foreach (var meta in device.Commands)
            {
                Commands.Add(new CommandItemViewModel(device, meta, CreateCommand));
            }

            ApplyState(device.State);
            _device.StateChanged += OnStateChanged;
        }

        public string DeviceId { get; }

        public ConnectionState ConnectionState
        {
            get => _connectionState;
            private set => SetProperty(ref _connectionState, value);
        }

        public HealthState HealthState
        {
            get => _healthState;
            private set => SetProperty(ref _healthState, value);
        }

        public string DisplayHealth =>
            ConnectionState == ConnectionState.Connected ? HealthState.ToString() : "N/A";

        public string? Detail
        {
            get => _detail;
            private set => SetProperty(ref _detail, value);
        }

        public ObservableCollection<CommandItemViewModel> Commands { get; }

        private void OnStateChanged(object? sender, DeviceStateSnapshot e)
        {
            if (_dispatcher.CheckAccess())
            {
                ApplyState(e);
            }
            else
            {
                _dispatcher.InvokeAsync(() => ApplyState(e));
            }
        }

        private void ApplyState(DeviceStateSnapshot state)
        {
            ConnectionState = state.ConnectionState;
            HealthState = state.HealthState;
            Detail = state.Detail;
            OnPropertyChanged(nameof(DisplayHealth));

            foreach (var cmd in Commands)
            {
                cmd.Refresh(state);
            }
        }

        private static IDeviceCommand CreateCommand(IDevice device, DeviceCommandMetadata metadata, object? parameter)
        {
            var enumType = metadata.CommandId.GetType();
            var deviceCommandType = typeof(DeviceCommand<>).MakeGenericType(enumType);
            var instance = Activator.CreateInstance(deviceCommandType, device.DeviceId, metadata.CommandId, parameter);
            if (instance is IDeviceCommand command)
            {
                return command;
            }

            throw new InvalidOperationException($"Unable to create command for {metadata.CommandId}");
        }
    }
}
