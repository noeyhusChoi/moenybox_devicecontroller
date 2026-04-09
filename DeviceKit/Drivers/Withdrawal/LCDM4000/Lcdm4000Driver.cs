using DeviceKit.Commands.Withdrawal;
using DeviceKit.Drivers.LCDM4000;
using DeviceKit.Drivers.Withdrawal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceKit.Drivers;

internal sealed class Lcdm4000Driver : DeviceDriverBase, IWithdrawalDriver
{
    private const int CassetteCount = 4;
    private const int StatusPayloadLength = 17;
    private const int DispenseResponsePayloadLength = 22;
    private const int DispenseTimeoutMs = 60000;

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
                "방출 경로 정리",
                static (driver, command, ct) => driver.EjectAsync(
                    command.Payload as WithdrawalEjectRequest ?? WithdrawalEjectRequest.Default,
                    ct),
                payloadValidator: static payload => payload is null || payload is WithdrawalEjectRequest),
            [WithdrawalCommands.DispenseName] = DeviceCommandSpec.Create<IWithdrawalDriver>(
                WithdrawalCommands.DispenseName,
                "지폐 방출",
                static (driver, command, ct) => driver.DispenseAsync((IReadOnlyList<WithdrawalDispenseSlotRequest>)command.Payload!, ct),
                payloadValidator: static payload => payload is IReadOnlyList<WithdrawalDispenseSlotRequest>),
            ["VERSION"] = DeviceCommandSpec.Create<Lcdm4000Driver>(
                "VERSION",
                "버전 조회",
                static (driver, _, ct) => driver.GetVersionAsync(ct))
        };

    private Lcdm4000Client? _client;

    protected override string ErrorTarget => "WITHDRAWAL";
    protected override IReadOnlyDictionary<string, DeviceCommandSpec> Commands => CommandTable;
    protected override bool IsCommandReady => _client is not null;

    public Lcdm4000Driver(DeviceDescriptor descriptor, ILogger<Lcdm4000Driver>? logger = null)
        : base(descriptor, logger ?? NullLogger<Lcdm4000Driver>.Instance)
    {
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await DisposeClientAsync().ConfigureAwait(false);

            var client = new Lcdm4000Client(Descriptor, Logger);
            _client = client;
            await client.StartAsync(ct).ConfigureAwait(false);

            var response = await QueryStatusAsync(client, ct).ConfigureAwait(false);
            return CreateSnapshot(BuildStatusAlerts(response));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            Logger.LogError(ex, "LCDM4000 initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        var client = _client ?? throw new InvalidOperationException("LCDM4000 not initialized.");
        var response = await QueryStatusAsync(client, ct).ConfigureAwait(false);
        return CreateSnapshot(BuildStatusAlerts(response));
    }

    public async Task<DeviceCommandResponse> ReadSensorsAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return NotConnected();

        var response = await QueryStatusAsync(_client, ct).ConfigureAwait(false);
        return ToCommandResponse(response, "STATUS");
    }

    public async Task<DeviceCommandResponse> InitializeHardwareAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return NotConnected();

        await _client.SendCommandAsync(
            Lcdm4000Command.Reset,
            Array.Empty<byte>(),
            processTimeoutMs: Lcdm4000Protocol.AckWaitMs,
            ct: ct,
            expectResponse: false).ConfigureAwait(false);

        await Task.Delay(Lcdm4000Protocol.ResetDelayMs, ct).ConfigureAwait(false);

        var response = await QueryStatusAsync(_client, ct).ConfigureAwait(false);
        return ToCommandResponse(response, "INIT");
    }

    public async Task<DeviceCommandResponse> EjectAsync(WithdrawalEjectRequest request, CancellationToken ct = default)
    {
        if (_client is null)
            return NotConnected();

        _ = request;

        var response = await _client.SendCommandAsync(
            Lcdm4000Command.Purge,
            Array.Empty<byte>(),
            processTimeoutMs: 60000,
            ct: ct).ConfigureAwait(false);

        return ToCommandResponse(response, "COMMAND");
    }

    public async Task<DeviceCommandResponse> DispenseAsync(IReadOnlyList<WithdrawalDispenseSlotRequest> requests, CancellationToken ct = default)
    {
        if (_client is null)
            return NotConnected();

        var payload = BuildDispensePayload(requests);

        var response = await _client.SendCommandAsync(
            Lcdm4000Command.Dispense,
            payload,
            processTimeoutMs: DispenseTimeoutMs,
            ct: ct).ConfigureAwait(false);

        return ToDispenseCommandResponse(response, requests);
    }

    public override async ValueTask DisposeAsync()
    {
        await DisposeClientAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private async Task<DeviceCommandResponse> GetVersionAsync(CancellationToken ct)
    {
        var client = _client ?? throw new InvalidOperationException("LCDM4000 not initialized.");

        var response = await client.SendCommandAsync(
            Lcdm4000Command.Supplementary,
            new[] { (byte)Lcdm4000SupplementaryCommand.Version, (byte)0x20, (byte)0x20, (byte)0x20 },
            processTimeoutMs: 3000,
            ct: ct).ConfigureAwait(false);

        return ToCommandResponse(response, "COMMAND");
    }

    private async Task<Lcdm4000Response> QueryStatusAsync(Lcdm4000Client client, CancellationToken ct)
        => await client.SendCommandAsync(
            Lcdm4000Command.Status,
            Array.Empty<byte>(),
            processTimeoutMs: 3000,
            ct: ct).ConfigureAwait(false);

    private DeviceCommandResponse NotConnected()
        => new(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED"));

    private DeviceCommandResponse ToCommandResponse(Lcdm4000Response response, string category)
    {
        if (response.Success)
            return new DeviceCommandResponse(true, Data: response.Data);

        return new DeviceCommandResponse(
            false,
            response.ErrorMessage ?? string.Empty,
            response.Data,
            new ErrorCode("DEV", ErrorTarget, category, Lcdm4000Protocol.GetErrorDetail(response.ErrorByte)));
    }

    private DeviceCommandResponse ToDispenseCommandResponse(Lcdm4000Response response, IReadOnlyList<WithdrawalDispenseSlotRequest> requests)
    {
        _ = requests;
        var result = new WithdrawalDispenseResult(
            Slots: TryParseDispenseSlotResults(response.Data) ?? Array.Empty<WithdrawalDispenseSlotResult>());

        if (response.Success)
            return new DeviceCommandResponse(true, Data: result);

        return new DeviceCommandResponse(
            false,
            response.ErrorMessage ?? string.Empty,
            result,
            new ErrorCode("DEV", ErrorTarget, "COMMAND", Lcdm4000Protocol.GetErrorDetail(response.ErrorByte)));
    }

    private IReadOnlyList<StatusEvent> BuildStatusAlerts(Lcdm4000Response response)
    {
        var alerts = new List<StatusEvent>();

        if (!response.Success)
        {
            alerts.Add(CreateAlert(
                new ErrorCode("DEV", ErrorTarget, "STATUS", Lcdm4000Protocol.GetErrorDetail(response.ErrorByte)),
                response.ErrorMessage ?? string.Empty,
                Severity.Error));
        }

        if (response.Data.Length < StatusPayloadLength - 1)
        {
            alerts.Add(CreateAlert(
                new ErrorCode("DEV", ErrorTarget, "STATUS", "INVALID_PAYLOAD"),
                $"LCDM4000 status payload length is invalid. len={response.Data.Length}",
                Severity.Warning));
            return alerts;
        }

        ParseDispenserStatus(response.Data[0], alerts);

        for (int slot = 0; slot < CassetteCount; slot++)
        {
            int offset = 1 + slot * 4;
            ParseCassetteStatus(slot + 1, response.Data[offset], response.Data[offset + 1], alerts);
        }

        return alerts;
    }

    private void ParseDispenserStatus(byte value, List<StatusEvent> alerts)
    {
        if (IsBitSet(value, 0)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "DVTL_BLOCKED"), string.Empty, Severity.Warning));
        if (IsBitSet(value, 1)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "DVTR_BLOCKED"), string.Empty, Severity.Warning));
        if (IsBitSet(value, 2)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "EJT_BLOCKED"), string.Empty, Severity.Warning));
        if (IsBitSet(value, 3)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "EXIT_BLOCKED"), string.Empty, Severity.Warning));
        if (IsBitSet(value, 4)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "RJT_BLOCKED"), string.Empty, Severity.Warning));
        if (IsBitSet(value, 5)) alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "SOL_BLOCKED"), string.Empty, Severity.Warning));
    }

    private void ParseCassetteStatus(int slot, byte stat, byte type, List<StatusEvent> alerts)
    {
        string prefix = $"CASSETTE_{slot}";
        bool cassettePresent = type != 0x30 && IsBitSet(stat, 2);

        if (IsBitSet(stat, 0))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", $"{prefix}_CHKL_BLOCKED"), string.Empty, Severity.Info));

        if (IsBitSet(stat, 1))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", $"{prefix}_CHKR_BLOCKED"), string.Empty, Severity.Info));

        if (!cassettePresent)
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", $"{prefix}_NOT_MOUNTED"), string.Empty, Severity.Warning));

        if (IsBitSet(stat, 3))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", $"{prefix}_NEAR_END"), string.Empty, Severity.Warning));
    }

    private async Task DisposeClientAsync()
    {
        if (_client is null)
            return;

        try
        {
            await _client.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        _client = null;
    }

    private static byte[] BuildDispensePayload(IReadOnlyList<WithdrawalDispenseSlotRequest> requests)
    {
        if (requests is null || requests.Count == 0)
            throw new InvalidOperationException("LCDM4000 dispense requires at least one slot request.");

        var counts = new int[CassetteCount];
        var totalRequested = 0;

        foreach (var request in requests)
        {
            if (request.Slot is < 1 or > CassetteCount)
                throw new InvalidOperationException($"Invalid LCDM4000 slot: {request.Slot}");

            if (request.Count < 0)
                throw new InvalidOperationException($"LCDM4000 slot count cannot be negative. slot={request.Slot}");

            counts[request.Slot - 1] += request.Count;
            totalRequested += request.Count;
        }

        if (totalRequested <= 0)
            throw new InvalidOperationException("LCDM4000 dispense count must be greater than zero.");

        if (totalRequested > 100)
            throw new InvalidOperationException($"LCDM4000 total dispense count cannot exceed 100. total={totalRequested}");

        var payload = new byte[15];
        payload[0] = Lcdm4000Protocol.EncodeCount(counts[0]);
        payload[1] = Lcdm4000Protocol.EncodeCount(counts[1]);
        payload[2] = Lcdm4000Protocol.EncodeCount(counts[2]);
        payload[3] = Lcdm4000Protocol.EncodeCount(counts[3]);
        payload[4] = 0x20;
        payload[5] = 0x20;

        for (int i = 6; i < payload.Length; i++)
            payload[i] = 0x20;

        return payload;
    }

    private static IReadOnlyList<WithdrawalDispenseSlotResult>? TryParseDispenseSlotResults(byte[] data)
    {
        if (data.Length < DispenseResponsePayloadLength)
            return null;

        var slotResults = new WithdrawalDispenseSlotResult[CassetteCount];
        for (int slot = 0; slot < CassetteCount; slot++)
        {
            int offset = 1 + slot * 3;
            byte exitCount = data[offset];
            byte rejectCount = data[offset + 1];
            slotResults[slot] = new WithdrawalDispenseSlotResult(
                Slot: slot + 1,
                SuccessCount: Lcdm4000Protocol.DecodeCount(exitCount),
                RejectCount: Lcdm4000Protocol.DecodeCount(rejectCount));
        }

        return slotResults;
    }

    private static bool IsBitSet(byte value, int bit) => ((value >> bit) & 0x01) == 0x01;

}
