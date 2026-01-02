using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Devices.Drivers.HCDM;
using KIOSK.Device.Transport;
using KIOSK.Device.Drivers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Devices.Drivers;

/// <summary>
/// HCDM-10K 드라이버: 정책/상태/명령 라우팅만 담당. 실제 프로토콜은 Hcdm10kClient에 위임.
/// </summary>
public sealed class Hcdm10kDriver : DeviceBase
{
    private Hcdm10kClient? _client;
    private CommandDispatcher? _dispatcher;
    private readonly ILogger<Hcdm10kDriver> _logger;

    public event Action<string>? Log;

    public Hcdm10kDriver(DeviceDescriptor desc, ITransport transport, ILogger<Hcdm10kDriver>? logger = null)
        : base(desc, transport)
    {
        _logger = logger ?? NullLogger<Hcdm10kDriver>.Instance;
    }

    public async override Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);
            await DisposeClientAsync().ConfigureAwait(false);

            var channel = CreateChannel(new Hcdm10kFramer());
            var client = new Hcdm10kClient(channel);
            client.Log += OnClientLog;
            _client = client;
            _dispatcher = new CommandDispatcher(
                Hcdm10kCommandHandlers.Create(client, Descriptor.DeviceKey),
                CreateUnknownCommandResult);

            // 장비 초기화 커맨드
            var initRes = await client.SendCommandAsync(Hcdm10kCommand.Initialize, new byte[] { 0x00 }, processTimeoutMs: 30000, ct: ct).ConfigureAwait(false);

            if (!initRes.Success)
                throw new InvalidOperationException("HCDM10K initialization failed.");

            return CreateSnapshot();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            _logger.LogError(ex, "HCDM10K initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public async override Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alerts = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _client ?? throw new InvalidOperationException("HCDM10K not initialized.");

            var res = await client.SendCommandAsync(Hcdm10kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 5000, ct: ct).ConfigureAwait(false);
            if (res.Success && res.Data is byte[] bytes && bytes.Length > 0)
            {
                ParseSensors(bytes, alerts);
            }
            else
            {
                alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"), string.Empty, Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "HCDM10K status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HCDM10K status failed. device={Device}", Name);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public async override Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
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
            _logger.LogWarning(ex, "HCDM10K command timeout. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "TIMEOUT"), Retryable: true);
        }
        catch (Exception ex)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogError(ex, "HCDM10K command failed. device={Device} command={Command}", Name, command.Name);
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

        try { _client.Log -= OnClientLog; } catch { }
        // _client는 IAsyncDisposable 아님
        _client = null;
        _dispatcher = null;
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);

    private void ParseSensors(byte[] x, List<StatusEvent> alerts)
    {
        if (x.Length <= (int)Hcdm10kSensorIndex.Cassette4)
            return;

        var shutter = x[(int)Hcdm10kSensorIndex.Shutter];
        if ((shutter & Hcdm10kShutterBits.ShutOpen) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_OPEN"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutClose) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_CLOSE"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutIn1) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_IN1"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutIn2) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_IN2"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutIn3) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_IN3"), string.Empty, Severity.Info));

        var status = x[(int)Hcdm10kSensorIndex.Status];
        if ((status & Hcdm10kStatusBits.Msol) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "MSOL_COLLECT"), string.Empty, Severity.Info));
        else
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "MSOL_DISPENSE"), string.Empty, Severity.Info));
        if ((status & Hcdm10kStatusBits.CisOpen) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CIS_OPEN"), string.Empty, Severity.Warning));
        if ((status & Hcdm10kStatusBits.RejectBoxOpen) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "REJECT_BOX_UNLOCK"), string.Empty, Severity.Warning));

        var gate = (Hcdm10kGateFlags)x[(int)Hcdm10kSensorIndex.Gate];
        if (gate.HasFlag(Hcdm10kGateFlags.Exit1Detected))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "EXIT1_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.RejectInDetected))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "REJECT_IN_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.Gate1Detected))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "GATE1_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.Gate2Detected))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "GATE2_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.ScanStart))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "SCAN_START"), string.Empty, Severity.Info));

        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette1], 1, alerts);
        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette2], 2, alerts);
        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette3], 3, alerts);
        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette4], 4, alerts);
    }

    private void ParseCassette(byte value, int cassetteNo, List<StatusEvent> alerts)
    {
        string name = $"카세트{cassetteNo}";

        if ((value & Hcdm10kCassetteBits.Skew1) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Info));
        if ((value & Hcdm10kCassetteBits.Skew2) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Info));
        if ((value & Hcdm10kCassetteBits.NearEnd) == 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if ((value & Hcdm10kCassetteBits.Mount) == 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NOT_MOUNTED"), string.Empty, Severity.Warning));
        if ((value & Hcdm10kCassetteBits.Id1A) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_ID1A"), string.Empty, Severity.Info));
        if ((value & Hcdm10kCassetteBits.Id2A) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_ID2A"), string.Empty, Severity.Info));
    }
}
