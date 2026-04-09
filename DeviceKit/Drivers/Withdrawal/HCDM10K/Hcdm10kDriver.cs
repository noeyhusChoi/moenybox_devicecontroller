using DeviceKit.Drivers.HCDM;
using DeviceKit.Drivers.Withdrawal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceKit.Drivers;

/// <summary>
/// HCDM-10K 드라이버: 정책/상태/명령 라우팅만 담당. 실제 프로토콜은 Hcdm10kClient에 위임.
/// </summary>
internal sealed class Hcdm10kDriver : DeviceDriverBase, IWithdrawalDriver
{
    public static IReadOnlyDictionary<string, DeviceCommandSpec> CommandTable { get; } =
        new Dictionary<string, DeviceCommandSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["RESTART"] = DeviceCommandSpec.Create<IDeviceDriver>(
                "RESTART",
                "재시작",
                static (_, _, _) => Task.FromResult(new DeviceCommandResponse(true))),
            [WithdrawalCommands.SensorName] = DeviceCommandSpec.Create<IWithdrawalDriver>(
                WithdrawalCommands.SensorName,
                "센서 조회",
                static (driver, _, ct) => driver.ReadSensorsAsync(ct)),
            [WithdrawalCommands.InitName] = DeviceCommandSpec.Create<IWithdrawalDriver>(
                WithdrawalCommands.InitName,
                "초기화",
                static (driver, _, ct) => driver.InitializeHardwareAsync(ct)),
            [WithdrawalCommands.EjectName] = DeviceCommandSpec.Create<IWithdrawalDriver>(
                WithdrawalCommands.EjectName,
                "방출/회수",
                static (driver, command, ct) => driver.EjectAsync(
                    command.Payload as WithdrawalEjectRequest ?? WithdrawalEjectRequest.Default,
                    ct),
                payloadValidator: static payload => payload is null || payload is WithdrawalEjectRequest),
            [WithdrawalCommands.DispenseName] = DeviceCommandSpec.Create<IWithdrawalDriver>(
                WithdrawalCommands.DispenseName,
                "지폐 방출",
                static (driver, command, ct) => driver.DispenseAsync((IReadOnlyList<WithdrawalDispenseSlotRequest>)command.Payload!, ct),
                payloadValidator: static payload => payload is IReadOnlyList<WithdrawalDispenseSlotRequest>),
        };

    private Hcdm10kClient? _client;
    protected override string ErrorTarget => "WITHDRAWAL";
    protected override IReadOnlyDictionary<string, DeviceCommandSpec> Commands => CommandTable;
    protected override bool IsCommandReady => _client is not null;

    public event Action<string>? Log;

    public Hcdm10kDriver(DeviceDescriptor desc, ILogger<Hcdm10kDriver>? logger = null)
        : base(desc, logger ?? NullLogger<Hcdm10kDriver>.Instance)
    {
    }

    public async override Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await DisposeClientAsync().ConfigureAwait(false);

            var client = new Hcdm10kClient(Descriptor, Logger);
            client.Log += OnClientLog;
            _client = client;

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
            Logger.LogError(ex, "HCDM10K initialize failed. device={Device} model={Model}", Name, Model);
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
                ParseStatus(bytes, alerts);
            }
            else
            {
                alerts.Add(CreateAlert(new ErrorCode("DEV", "WITHDRAWAL", "STATUS", "ERROR"), string.Empty, Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            Logger.LogWarning(ex, "HCDM10K status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", "WITHDRAWAL", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "HCDM10K status failed. device={Device}", Name);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public Task<DeviceCommandResponse> ReadSensorsAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.SendCommandAsync(Hcdm10kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 5000, ct: ct);
    }

    public Task<DeviceCommandResponse> InitializeHardwareAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.SendCommandAsync(Hcdm10kCommand.Initialize, new byte[] { 0x00 }, processTimeoutMs: 30000, ct: ct);
    }

    public Task<DeviceCommandResponse> EjectAsync(WithdrawalEjectRequest request, CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        _ = request;

        return _client.SendCommandAsync(
            Hcdm10kCommand.Eject,
            WithdrawalEjectRequest.Default.ToPayload(),
            processTimeoutMs: 10000,
            ct: ct);
    }

    public Task<DeviceCommandResponse> DispenseAsync(IReadOnlyList<WithdrawalDispenseSlotRequest> requests, CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.SendCommandAsync(
            Hcdm10kCommand.Dispense,
            BuildDispensePayload(requests),
            processTimeoutMs: 120000,
            ct: ct);
    }

    private static byte[] BuildDispensePayload(IReadOnlyList<WithdrawalDispenseSlotRequest> requests)
    {
        var payload = new byte[15];

        foreach (var request in requests)
        {
            var index = request.Slot - 1;
            if (index < 0 || index >= payload.Length)
                throw new InvalidOperationException($"Invalid HCDM10K slot: {request.Slot}");

            checked
            {
                payload[index] += (byte)request.Count;
            }
        }

        return payload;
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
        try { await _client.DisposeAsync().ConfigureAwait(false); } catch { }
        _client = null;
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);

    private Hcdm10kClient GetRequiredClient()
        => _client ?? throw new InvalidOperationException("HCDM10K not initialized.");

    private void ParseStatus(byte[] sensors, List<StatusEvent> alerts)
    {
        if (sensors.Length <= (int)Hcdm10kSensorIndex.Cassette4)
            return;

        var shutter = sensors[(int)Hcdm10kSensorIndex.Shutter];
        if ((shutter & Hcdm10kShutterBits.ShutOpen) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "SHUT_OPEN"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutClose) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "SHUT_CLOSE"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutIn1) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "SHUT_IN1"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutIn2) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "SHUT_IN2"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutIn3) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "SHUT_IN3"), string.Empty, Severity.Info));

        var status = sensors[(int)Hcdm10kSensorIndex.Status];
        if ((status & Hcdm10kStatusBits.Msol) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "MSOL_COLLECT"), string.Empty, Severity.Info));
        else
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "MSOL_DISPENSE"), string.Empty, Severity.Info));
        if ((status & Hcdm10kStatusBits.CisOpen) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CIS_OPEN"), string.Empty, Severity.Warning));
        if ((status & Hcdm10kStatusBits.RejectBoxOpen) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "REJECT_BOX_UNLOCK"), string.Empty, Severity.Warning));

        var gate = (Hcdm10kGateFlags)sensors[(int)Hcdm10kSensorIndex.Gate];
        if (gate.HasFlag(Hcdm10kGateFlags.Exit1Detected))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "EXIT1_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.RejectInDetected))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "REJECT_IN_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.Gate1Detected))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "GATE1_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.Gate2Detected))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "GATE2_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.ScanStart))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "SCAN_START"), string.Empty, Severity.Info));

        ParseCassetteStatus(sensors[(int)Hcdm10kSensorIndex.Cassette1], alerts);
        ParseCassetteStatus(sensors[(int)Hcdm10kSensorIndex.Cassette2], alerts);
        ParseCassetteStatus(sensors[(int)Hcdm10kSensorIndex.Cassette3], alerts);
        ParseCassetteStatus(sensors[(int)Hcdm10kSensorIndex.Cassette4], alerts);
    }

    private void ParseCassetteStatus(byte value, List<StatusEvent> alerts)
    {
        if ((value & Hcdm10kCassetteBits.Skew1) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Info));
        if ((value & Hcdm10kCassetteBits.Skew2) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Info));
        if ((value & Hcdm10kCassetteBits.NearEnd) == 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if ((value & Hcdm10kCassetteBits.Mount) == 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_NOT_MOUNTED"), string.Empty, Severity.Warning));
        if ((value & Hcdm10kCassetteBits.Id1A) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_ID1A"), string.Empty, Severity.Info));
        if ((value & Hcdm10kCassetteBits.Id2A) != 0)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_ID2A"), string.Empty, Severity.Info));
    }
}
