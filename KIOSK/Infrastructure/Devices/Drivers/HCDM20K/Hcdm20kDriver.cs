using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Devices.Drivers.HCDM20K;
using KIOSK.Device.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Devices.Drivers;

/// <summary>
/// HCDM-20K 드라이버: 정책/상태/명령 라우팅만 담당하고, 실제 프로토콜은 Hcdm20kClient에 위임한다.
/// </summary>
public sealed class Hcdm20kDriver : DeviceBase
{
    private Hcdm20kClient? _client;
    private CommandDispatcher? _dispatcher;
    private readonly ILogger<Hcdm20kDriver> _logger;

    public Hcdm20kDriver(DeviceDescriptor desc, ITransport transport, ILogger<Hcdm20kDriver>? logger = null)
        : base(desc, transport)
    {
        _logger = logger ?? NullLogger<Hcdm20kDriver>.Instance;
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);
            await DisposeClientAsync().ConfigureAwait(false);

            var channel = CreateChannel(new Hcdm20kFramer());
            var client = new Hcdm20kClient(channel);
            _client = client;
            _dispatcher = new CommandDispatcher(
                Hcdm20kCommandHandlers.Create(client, Descriptor.DeviceKey),
                CreateUnknownCommandResult);
            await client.StartAsync(ct).ConfigureAwait(false);

            var initData = BuildInitPayload(cassetteCount: 4);
            var initRes = await client.SendCommandAsync(
                Hcdm20kCommand.Initialize,
                initData,
                processTimeoutMs: 8000,
                ct: ct).ConfigureAwait(false);

            if (!initRes.Success)
                throw new InvalidOperationException("HCDM20K initialization failed.");

            return CreateSnapshot();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            _logger.LogError(ex, "HCDM20K initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alerts = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            var client = _client ?? throw new InvalidOperationException("HCDM20K not initialized.");

            var res = await client.SendCommandAsync(Hcdm20kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 2000, ct: ct).ConfigureAwait(false);
            if (!res.Success || res.Data is not byte[] data)
            {
                alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"), string.Empty, Severity.Warning));
            }
            else if (data.Length >= 16)
            {
                ParseSensors(data, alerts);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "HCDM20K status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HCDM20K status failed. device={Device}", Name);
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
        catch (TimeoutException ex)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogWarning(ex, "HCDM20K command timeout. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "TIMEOUT"), Retryable: true);
        }
        catch (Exception ex)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogError(ex, "HCDM20K command failed. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "ERROR"));
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await DisposeClientAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private async Task DisposeClientAsync()
    {
        if (_client is null)
            return;

        try { await _client.DisposeAsync().ConfigureAwait(false); } catch { }
        _client = null;
        _dispatcher = null;
    }

    private static byte[] BuildInitPayload(int cassetteCount)
    {
        var initData = new List<string>
        {
            "0", // unread tolerance
            "0", // country: Korea
            cassetteCount.ToString(),
            "0"  // anti-counterfeit check
        };

        for (int i = 0; i < cassetteCount; i++)
            initData.Add("0");

        return Encoding.ASCII.GetBytes(string.Concat(initData));
    }

    private static bool BitIsSet(byte b, int bit) => ((b >> bit) & 0x01) == 0x01;

    private void ParseSensors(byte[] data, List<StatusEvent> alerts)
    {
        if (BitIsSet(data[0], 5)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 4)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 3)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 2)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 1)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 0)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));

        if (BitIsSet(data[3], 5)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 4)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 3)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 2)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 1)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 0)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));

        if (BitIsSet(data[4], 5)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 4)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 3)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 2)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 1)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 0)) alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));

        if (data[6] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "GATE1_DETECTED"), string.Empty, Severity.Warning));
        if (data[7] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "GATE2_DETECTED"), string.Empty, Severity.Warning));
        if (data[9] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "EXIT1_DETECTED"), string.Empty, Severity.Warning));
        if (data[10] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "REJECT_IN_DETECTED"), string.Empty, Severity.Warning));
        if (data[11] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "REJECT_BOX_UNLOCK"), string.Empty, Severity.Warning));
        if (data[12] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CIS_OPEN"), string.Empty, Severity.Warning));
    }

}
