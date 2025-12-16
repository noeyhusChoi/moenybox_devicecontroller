using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceController.Core.Abstractions;
using DeviceController.Core.Commands;
using DeviceController.Core.States;

namespace DeviceController.ViewModels
{
    public class CommandItemViewModel : ObservableObject
    {
        private readonly IDevice _device;
        private readonly DeviceCommandMetadata _metadata;
        private readonly Func<IDevice, DeviceCommandMetadata, object?, IDeviceCommand> _commandFactory;
        private string? _parameterText;
        private string? _lastMessage;
        private bool _isAllowed;

        public CommandItemViewModel(IDevice device, DeviceCommandMetadata metadata, Func<IDevice, DeviceCommandMetadata, object?, IDeviceCommand> commandFactory)
        {
            _device = device;
            _metadata = metadata;
            _commandFactory = commandFactory;
            ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
        }

        public string DisplayName => _metadata.DisplayName;
        public string Description => _metadata.Description;
        public bool IsStatus => _metadata.IsStatusCommand;
        public Type? ParameterType => _metadata.ParameterType;

        public string? ParameterText
        {
            get => _parameterText;
            set => SetProperty(ref _parameterText, value);
        }

        public string? LastMessage
        {
            get => _lastMessage;
            private set => SetProperty(ref _lastMessage, value);
        }

        public bool IsAllowed
        {
            get => _isAllowed;
            private set => SetProperty(ref _isAllowed, value);
        }

        public IAsyncRelayCommand ExecuteCommand { get; }

        public void Refresh(DeviceStateSnapshot state)
        {
            IsAllowed = _device.CanExecute(_metadata.CommandId);
            ExecuteCommand.NotifyCanExecuteChanged();
        }

        private bool CanExecute()
        {
            return IsAllowed;
        }

        private async Task ExecuteAsync()
        {
            object? parameter = null;
            if (_metadata.ParameterType != null)
            {
                var parseResult = TryParseParameter(_metadata.ParameterType, ParameterText);
                if (!parseResult.success)
                {
                    LastMessage = parseResult.message ?? "Invalid parameter.";
                    return;
                }

                parameter = parseResult.value;
            }

            var command = _commandFactory(_device, _metadata, parameter);
            var result = await _device.EnqueueAsync(command, CancellationToken.None);
            LastMessage = $"{result.Status}: {result.Message}";
        }

        private static (bool success, object? value, string? message) TryParseParameter(Type targetType, string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, null, "Parameter required.");
            }

            try
            {
                var converted = ConvertToTarget(targetType, text);
                return (true, converted, null);
            }
            catch
            {
                return (false, null, $"Parameter must be {targetType.Name}.");
            }
        }

        private static object ConvertToTarget(Type targetType, string text)
        {
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(string))
            {
                return text;
            }

            if (underlying.IsEnum)
            {
                return Enum.Parse(underlying, text, ignoreCase: true);
            }

            if (underlying.IsPrimitive || underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float))
            {
                return Convert.ChangeType(text, underlying, CultureInfo.InvariantCulture)!;
            }

            var ctor = underlying.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 1);
            if (ctor != null)
            {
                var parameterType = ctor.GetParameters()[0].ParameterType;
                var arg = ConvertToTarget(parameterType, text);
                return ctor.Invoke(new[] { arg });
            }

            throw new InvalidOperationException($"Unsupported parameter type {targetType.Name}");
        }
    }
}
