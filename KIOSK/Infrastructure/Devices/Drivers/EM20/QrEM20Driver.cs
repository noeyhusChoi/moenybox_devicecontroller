using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.EM20;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Device.Drivers;

public sealed class QrEM20Driver : DeviceBase
{
    private Em20Client? _client;
    private CommandDispatcher? _dispatcher;
    private readonly ILogger<QrEM20Driver> _logger;

    public QrEM20Driver(DeviceDescriptor desc, ITransport transport, ILogger<QrEM20Driver>? logger = null)
        : base(desc, transport)
    {
        _logger = logger ?? NullLogger<QrEM20Driver>.Instance;
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);
            await DisposeClientAsync().ConfigureAwait(false);

            var client = new Em20Client(RequireTransport());
            _client = client;
            _dispatcher = new CommandDispatcher(
                Em20CommandHandlers.Create(client),
                CreateUnknownCommandResult);
            await client.StartAsync(ct).ConfigureAwait(false);

            return CreateSnapshot();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            _logger.LogError(ex, "EM20 initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alerts = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _client ?? throw new InvalidOperationException("EM20 client not initialized.");
            var result = await client.RequestStatusAsync(ct).ConfigureAwait(false);
            if (!result.Success)
                alerts.Add(CreateAlert(new ErrorCode("DEV", "QR", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            alerts.Add(CreateAlert(new ErrorCode("DEV", "QR", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EM20 status failed. device={Device} model={Model}", Name, Model);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;

            if (_client is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "NOT_CONNECTED"));

            if (_dispatcher is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "NOT_CONNECTED"));

            return await _dispatcher.DispatchAsync(command, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogWarning("EM20 command timeout. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "TIMEOUT"), Retryable: true);
        }
        catch (Exception ex)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogError(ex, "EM20 command failed. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "ERROR"));
        }

    }

    public override async ValueTask DisposeAsync()
    {
        await DisposeClientAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private Task DisposeClientAsync()
    {
        _client = null;
        _dispatcher = null;
        return Task.CompletedTask;
    }
}
