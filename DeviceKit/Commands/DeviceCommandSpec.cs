using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceKit.Commands;

internal sealed class DeviceCommandSpec
{
    private readonly Func<object?, bool>? _payloadValidator;
    private readonly Func<IDeviceDriver, DeviceCommandRequest, CancellationToken, Task<DeviceCommandResponse>> _execute;

    public DeviceCommandSpec(
        string name,
        string description,
        Func<IDeviceDriver, DeviceCommandRequest, CancellationToken, Task<DeviceCommandResponse>> execute,
        Func<object?, bool>? payloadValidator = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _payloadValidator = payloadValidator;
    }

    public string Name { get; }
    public string Description { get; }

    public bool IsPayloadValid(object? payload) => _payloadValidator?.Invoke(payload) ?? true;

    public Task<DeviceCommandResponse> ExecuteAsync(IDeviceDriver driver, DeviceCommandRequest command, CancellationToken ct)
        => _execute(driver, command, ct);

    public static DeviceCommandSpec Create<TDriver>(
        string name,
        string description,
        Func<TDriver, DeviceCommandRequest, CancellationToken, Task<DeviceCommandResponse>> execute,
        Func<object?, bool>? payloadValidator = null)
        where TDriver : class, IDeviceDriver
    {
        if (execute is null)
            throw new ArgumentNullException(nameof(execute));

        return new DeviceCommandSpec(
            name,
            description,
            (driver, command, ct) =>
            {
                if (driver is not TDriver typedDriver)
                    throw new InvalidOperationException($"Driver type mismatch for command '{name}'.");

                return execute(typedDriver, command, ct);
            },
            payloadValidator);
    }
}
