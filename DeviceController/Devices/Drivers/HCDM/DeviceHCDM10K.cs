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
public sealed class DeviceHCDM10K : DeviceBase
{
    private Hcdm10kClient? _client;

    public event Action<string>? Log;

    public DeviceHCDM10K(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
    }

    public async override Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
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
                : CreateSnapshot(new[] { CreateAlarm("00", "미연결", Severity.Error) });
        }
        catch (Exception)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            return CreateSnapshot(new[]
            {
                CreateAlarm("00", "미연결", Severity.Error)
            });
        }
    }

    public async override Task<DeviceStatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alarms = new List<DeviceAlarm>();

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
                alarms.Add(CreateAlarm("01", "통신오류", Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
        }

        return CreateSnapshot(alarms);
    }

    public async override Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            if (_client is null)
                return new CommandResult(false, "Device not connected");

            var client = _client;

            switch (command)
            {
                case { Name: string name, Payload: byte[] data } when name.Equals("SENSOR", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm10kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 5000, ct: ct).ConfigureAwait(false);

                case { Name: string name, Payload: byte[] data } when name.Equals("INIT", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm10kCommand.Initialize, new byte[] { 0x00 }, processTimeoutMs: 30000, ct: ct).ConfigureAwait(false);

                case { Name: string name, Payload: byte[] data } when name.Equals("DISPENSE", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm10kCommand.Dispense, data, processTimeoutMs: 120000, ct: ct).ConfigureAwait(false);
                case { Name: string name, Payload: byte[] data } when name.Equals("EJECT", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm10kCommand.Eject, data, processTimeoutMs: 10000, ct: ct).ConfigureAwait(false);
                default:
                    return new CommandResult(false, $"[{command.Name}] UNKNOWN COMMAND");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"[{command.Name}] ERROR COMMAND: {ex.Message}");
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

    private void ParseSensors(byte[] x, List<DeviceAlarm> alarms)
    {
        if (x.Length <= (int)Hcdm10kSensorIndex.Cassette4)
            return;

        var door = x[(int)Hcdm10kSensorIndex.Door];
        if ((door & Hcdm10kDoorBits.RejectBoxOpen) != 0)
            alarms.Add(CreateAlarm("01", "리젝트 박스 열림", Severity.Warning));

        var gate = (Hcdm10kGateFlags)x[(int)Hcdm10kSensorIndex.Gate];
        if (gate.HasFlag(Hcdm10kGateFlags.EjectDetected))
            alarms.Add(CreateAlarm("01", "방출 센서 감지", Severity.Warning));
        if (gate.HasFlag(Hcdm10kGateFlags.CollectDetected))
            alarms.Add(CreateAlarm("01", "회수 센서 감지", Severity.Warning));
        if (gate.HasFlag(Hcdm10kGateFlags.Gate1Detected))
            alarms.Add(CreateAlarm("01", "GATE1 센서 감지", Severity.Warning));
        if (gate.HasFlag(Hcdm10kGateFlags.Gate2Detected))
            alarms.Add(CreateAlarm("01", "GATE2 센서 감지", Severity.Warning));

        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette1], 1, alarms);
        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette2], 2, alarms);
        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette3], 3, alarms);
        ParseCassette(x[(int)Hcdm10kSensorIndex.Cassette4], 4, alarms);
    }

    private void ParseCassette(byte value, int cassetteNo, List<DeviceAlarm> alarms)
    {
        string name = $"카세트{cassetteNo}";

        if ((value & Hcdm10kCassetteBits.Skew1) != 0)
            alarms.Add(CreateAlarm("01", $"{name} SKEW1 센서 감지", Severity.Warning));
        if ((value & Hcdm10kCassetteBits.Present) != 0)
            alarms.Add(CreateAlarm("01", $"{name} 센서 감지", Severity.Warning));
        if ((value & Hcdm10kCassetteBits.LowLevel) == 0)
            alarms.Add(CreateAlarm("01", $"{name} 시재 부족 감지", Severity.Warning));
        if ((value & Hcdm10kCassetteBits.Mounted) == 0)
            alarms.Add(CreateAlarm("01", $"{name} 미장착 감지", Severity.Warning));
        if ((value & Hcdm10kCassetteBits.DipSwitch1) != 0)
            alarms.Add(CreateAlarm("01", $"{name} 딥스위치1 센서 감지", Severity.Warning));
        if ((value & Hcdm10kCassetteBits.DipSwitch2) != 0)
            alarms.Add(CreateAlarm("01", $"{name} 딥스위치2 센서 감지", Severity.Warning));
    }
}
