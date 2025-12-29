using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Devices.Drivers.HCDM;
using KIOSK.Device.Transport;
using KIOSK.Device.Drivers;

namespace KIOSK.Devices.Drivers;

/// <summary>
/// HCDM-10K 드라이버: 정책/상태/명령 라우팅만 담당. 실제 프로토콜은 Hcdm10kClient에 위임.
/// </summary>
public sealed class Hcdm10kDriver : DeviceBase
{
    private Hcdm10kClient? _client;

    public event Action<string>? Log;

    public Hcdm10kDriver(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
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

            // 장비 초기화 커맨드
            var initRes = await client.SendCommandAsync(Hcdm10kCommand.Initialize, new byte[] { 0x00 }, processTimeoutMs: 30000, ct: ct).ConfigureAwait(false);

            return initRes.Success
                ? CreateSnapshot()
                : CreateSnapshot(new[]
                {
                    CreateAlarm(new ErrorCode("DEV", "HCDM", "CONNECT", "FAIL"), string.Empty, Severity.Error)
                });
        }
        catch (Exception)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            return CreateSnapshot(new[]
            {
                CreateAlarm(new ErrorCode("DEV", "HCDM", "CONNECT", "FAIL"), string.Empty, Severity.Error)
            });
        }
    }

    public async override Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alarms = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _client ?? throw new InvalidOperationException("HCDM10K not initialized.");

            var res = await client.SendCommandAsync(Hcdm10kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 5000, ct: ct).ConfigureAwait(false);
            if (res.Success && res.Data is byte[] bytes && bytes.Length > 0)
            {
                ParseSensors(bytes, alarms);
            }
            else
            {
                alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"), string.Empty, Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"), string.Empty, Severity.Warning));
        }

        return CreateSnapshot(alarms);
    }

    public async override Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            if (_client is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "HCDM", "CONNECT", "FAIL"));

            var client = _client;

            switch (command)
            {
                case { Name: string name } when name.Equals("RESTART", StringComparison.OrdinalIgnoreCase):
                    return new CommandResult(true);

                case { Name: string name, Payload: byte[] data } when name.Equals("SENSOR", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm10kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 5000, ct: ct).ConfigureAwait(false);

                case { Name: string name, Payload: byte[] data } when name.Equals("INIT", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm10kCommand.Initialize, new byte[] { 0x00 }, processTimeoutMs: 30000, ct: ct).ConfigureAwait(false);

                case { Name: string name, Payload: byte[] data } when name.Equals("DISPENSE", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm10kCommand.Dispense, data, processTimeoutMs: 120000, ct: ct).ConfigureAwait(false);
                case { Name: string name, Payload: byte[] data } when name.Equals("EJECT", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm10kCommand.Eject, data, processTimeoutMs: 10000, ct: ct).ConfigureAwait(false);
                default:
                    return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "HCDM", "ERROR", "UNKNOWN_COMMAND"));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"));
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
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);

    private void ParseSensors(byte[] x, List<StatusEvent> alarms)
    {
        if (x.Length <= (int)Hcdm10kSensorIndex.Cassette4)
            return;

        var shutter = x[(int)Hcdm10kSensorIndex.Shutter];
        if ((shutter & Hcdm10kShutterBits.ShutOpen) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_OPEN"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutClose) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_CLOSE"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutIn1) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_IN1"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutIn2) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_IN2"), string.Empty, Severity.Info));
        if ((shutter & Hcdm10kShutterBits.ShutIn3) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "SHUT_IN3"), string.Empty, Severity.Info));

        var status = x[(int)Hcdm10kSensorIndex.Status];
        if ((status & Hcdm10kStatusBits.Msol) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "MSOL_COLLECT"), string.Empty, Severity.Info));
        else
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "MSOL_DISPENSE"), string.Empty, Severity.Info));
        if ((status & Hcdm10kStatusBits.CisOpen) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CIS_OPEN"), string.Empty, Severity.Warning));
        if ((status & Hcdm10kStatusBits.RejectBoxOpen) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "REJECT_BOX_UNLOCK"), string.Empty, Severity.Warning));

        var gate = (Hcdm10kGateFlags)x[(int)Hcdm10kSensorIndex.Gate];
        if (gate.HasFlag(Hcdm10kGateFlags.Exit1Detected))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "EXIT1_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.RejectInDetected))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "REJECT_IN_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.Gate1Detected))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "GATE1_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.Gate2Detected))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "GATE2_DETECTED"), string.Empty, Severity.Info));
        if (gate.HasFlag(Hcdm10kGateFlags.ScanStart))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "SCAN_START"), string.Empty, Severity.Info));

        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette1], 1, alarms);
        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette2], 2, alarms);
        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette3], 3, alarms);
        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette4], 4, alarms);
    }

    private void ParseCassette(byte value, int cassetteNo, List<StatusEvent> alarms)
    {
        string name = $"카세트{cassetteNo}";

        if ((value & Hcdm10kCassetteBits.Skew1) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Info));
        if ((value & Hcdm10kCassetteBits.Skew2) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Info));
        if ((value & Hcdm10kCassetteBits.NearEnd) == 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if ((value & Hcdm10kCassetteBits.Mount) == 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NOT_MOUNTED"), string.Empty, Severity.Warning));
        if ((value & Hcdm10kCassetteBits.Id1A) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_ID1A"), string.Empty, Severity.Info));
        if ((value & Hcdm10kCassetteBits.Id2A) != 0)
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_ID2A"), string.Empty, Severity.Info));
    }
}
