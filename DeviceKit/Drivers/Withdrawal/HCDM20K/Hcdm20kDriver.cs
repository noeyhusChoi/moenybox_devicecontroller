using DeviceKit.Drivers.HCDM20K;
using DeviceKit.Drivers.Withdrawal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace DeviceKit.Drivers;

/// <summary>
/// HCDM-20K 드라이버: 정책/상태/명령 라우팅만 담당하고, 실제 프로토콜은 Hcdm20kClient에 위임한다.
/// </summary>
internal sealed class Hcdm20kDriver : DeviceDriverBase, IWithdrawalDriver
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
            ["VERSION"] = DeviceCommandSpec.Create<Hcdm20kDriver>(
                "VERSION",
                "버전 조회",
                static (driver, _, ct) => driver.GetRequiredClient().SendCommandAsync(Hcdm20kCommand.Version, Array.Empty<byte>(), processTimeoutMs: 2000, ct: ct)),
        };

    private Hcdm20kClient? _client;
    protected override string ErrorTarget => "WITHDRAWAL";
    protected override IReadOnlyDictionary<string, DeviceCommandSpec> Commands => CommandTable;
    protected override bool IsCommandReady => _client is not null;

    public Hcdm20kDriver(DeviceDescriptor desc, ILogger<Hcdm20kDriver>? logger = null)
        : base(desc, logger ?? NullLogger<Hcdm20kDriver>.Instance)
    {
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await DisposeClientAsync().ConfigureAwait(false);

            var client = new Hcdm20kClient(Descriptor, Logger);
            _client = client;
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
            Logger.LogError(ex, "HCDM20K initialize failed. device={Device} model={Model}", Name, Model);
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
                alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "ERROR"), res.Message ?? "Withdrawal status request failed.", Severity.Warning));
            }
            else if (data.Length >= 16)
            {
                ParseStatus(data, alerts);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            Logger.LogWarning(ex, "HCDM20K status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "TIMEOUT"), ex.Message, Severity.Warning));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "HCDM20K status failed. device={Device}", Name);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public Task<DeviceCommandResponse> ReadSensorsAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.SendCommandAsync(
            Hcdm20kCommand.Sensor,
            Array.Empty<byte>(),
            processTimeoutMs: 2000,
            ct: ct);
    }

    public Task<DeviceCommandResponse> InitializeHardwareAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.SendCommandAsync(
            Hcdm20kCommand.Initialize,
            BuildInitPayload(cassetteCount: 4),
            processTimeoutMs: 8000,
            ct: ct);
    }

    public Task<DeviceCommandResponse> EjectAsync(WithdrawalEjectRequest request, CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.SendCommandAsync(
            Hcdm20kCommand.Eject,
            (request ?? WithdrawalEjectRequest.Default).ToPayload(),
            processTimeoutMs: 5000,
            ct: ct);
    }

    public Task<DeviceCommandResponse> DispenseAsync(IReadOnlyList<WithdrawalDispenseSlotRequest> requests, CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        var payload = BuildDispensePayload(requests);
        int estimatedCount = EstimateTotalRequestedFromPayload(payload);
        int timeoutMs = (int)((estimatedCount / 3.0 + 5) * 1000);
        return _client.SendCommandAsync(
            Hcdm20kCommand.Dispense,
            payload,
            processTimeoutMs: Math.Max(timeoutMs, 15000),
            ct: ct,
            isLongOpWithEnq: true);
    }

    private static byte[] BuildDispensePayload(IReadOnlyList<WithdrawalDispenseSlotRequest> requests)
    {
        var ordered = requests
            .Where(x => x.Count > 0)
            .OrderBy(x => x.Slot)
            .ToArray();

        var builder = new StringBuilder();
        builder.Append(ordered.Length);

        foreach (var request in ordered)
        {
            if (request.Slot is < 0 or > 9)
                throw new InvalidOperationException($"Invalid HCDM20K slot: {request.Slot}");

            builder.Append(request.Slot);
            builder.Append(request.Count.ToString("000"));
        }

        return Encoding.ASCII.GetBytes(builder.ToString());
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

    private Hcdm20kClient GetRequiredClient()
        => _client ?? throw new InvalidOperationException("HCDM20K not initialized.");

    private static int EstimateTotalRequestedFromPayload(byte[] payload)
    {
        if (payload.Length == 0) return 0;
        try
        {
            string s = Encoding.ASCII.GetString(payload);
            if (s.Length == 0) return 0;

            int i = 0;
            int total = 0;

            if (i < s.Length && char.IsDigit(s[i]))
            {
                int n = s[i] - '0';
                i++;
                for (int k = 0; k < n; k++)
                {
                    if (i + 4 <= s.Length)
                    {
                        i += 1;
                        if (int.TryParse(s.AsSpan(i, Math.Min(3, s.Length - i)), out int c))
                            total += c;
                        i += 3;
                    }
                }
            }

            return total;
        }
        catch
        {
            return 0;
        }
    }

    private void ParseStatus(byte[] data, List<StatusEvent> alerts)
    {
        if (BitIsSet(data[0], 5)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 4)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 3)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 2)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 1)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 0)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));

        if (BitIsSet(data[3], 5)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 4)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 3)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 2)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 1)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 0)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));

        if (BitIsSet(data[4], 5)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 4)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 3)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 2)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 1)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 0)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));

        if (data[6] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "GATE1_DETECTED"), string.Empty, Severity.Warning));
        if (data[7] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "GATE2_DETECTED"), string.Empty, Severity.Warning));
        if (data[9] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "EXIT1_DETECTED"), string.Empty, Severity.Warning));
        if (data[10] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "REJECT_IN_DETECTED"), string.Empty, Severity.Warning));
        if (data[11] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "REJECT_BOX_UNLOCK"), string.Empty, Severity.Warning));
        if (data[12] == '1') alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CIS_OPEN"), string.Empty, Severity.Warning));
    }

    private static bool BitIsSet(byte value, int bit) => ((value >> bit) & 0x01) == 0x01;
}
