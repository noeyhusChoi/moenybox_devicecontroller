using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Devices.Drivers.HCDM20K;
using KIOSK.Device.Transport;

namespace KIOSK.Devices.Drivers;

/// <summary>
/// HCDM-20K 드라이버: 정책/상태/명령 라우팅만 담당하고, 실제 프로토콜은 Hcdm20kClient에 위임한다.
/// </summary>
public sealed class Hcdm20kDriver : DeviceBase
{
    private Hcdm20kClient? _client;

    public Hcdm20kDriver(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
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
            await client.StartAsync(ct).ConfigureAwait(false);

            var initData = BuildInitPayload(cassetteCount: 4);
            var initRes = await client.SendCommandAsync(
                Hcdm20kCommand.Initialize,
                initData,
                processTimeoutMs: 8000,
                ct: ct).ConfigureAwait(false);

            return initRes.Success
                ? CreateSnapshot()
                : CreateSnapshot(new[]
                {
                    CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"), string.Empty, Severity.Error)
                });
        }
        catch
        {
            return CreateSnapshot(new[]
            {
                CreateAlarm(new ErrorCode("DEV", "HCDM", "CONNECT", "FAIL"), string.Empty, Severity.Error)
            });
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alarms = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            var client = _client ?? throw new InvalidOperationException("HCDM20K not initialized.");

            var res = await client.SendCommandAsync(Hcdm20kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 2000, ct: ct).ConfigureAwait(false);
            if (!res.Success || res.Data is not byte[] data)
            {
                alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"), string.Empty, Severity.Warning));
            }
            else if (data.Length >= 16)
            {
                ParseSensors(data, alarms);
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

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
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

                case { Name: string name } when name.Equals("SENSOR", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm20kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 2000, ct: ct).ConfigureAwait(false);

                case { Name: string name, Payload: byte[] data } when name.Equals("INIT", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm20kCommand.Initialize, data, processTimeoutMs: 8000, ct: ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("VERSION", StringComparison.OrdinalIgnoreCase):
                    return await client.SendCommandAsync(Hcdm20kCommand.Version, Array.Empty<byte>(), processTimeoutMs: 2000, ct: ct).ConfigureAwait(false);

                case { Name: string name, Payload: byte[] data } when name.Equals("EJECT", StringComparison.OrdinalIgnoreCase):
                    {
                        var args = new[] { (data != null && data.Length > 0) ? Encoding.ASCII.GetString(data) : "0" };
                        var payload = Encoding.ASCII.GetBytes(string.Concat(args));
                        return await client.SendCommandAsync(Hcdm20kCommand.Eject, payload, processTimeoutMs: 5000, ct: ct).ConfigureAwait(false);
                    }

                case { Name: string name, Payload: byte[] data } when name.Equals("DISPENSE", StringComparison.OrdinalIgnoreCase):
                    {
                        int estimatedCount = EstimateTotalRequestedFromPayload(data);
                        int timeoutMs = (int)((estimatedCount / 3.0 + 5) * 1000);
                        return await client.SendCommandAsync(
                            Hcdm20kCommand.Dispense,
                            data,
                            processTimeoutMs: Math.Max(timeoutMs, 15000),
                            ct: ct,
                            isLongOpWithEnq: true).ConfigureAwait(false);
                    }
                default:
                    return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "HCDM", "ERROR", "UNKNOWN_COMMAND"));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
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

    private static bool BitIsSet(byte b, int bit) => ((b >> bit) & 0x01) == 0x01;

    private void ParseSensors(byte[] data, List<StatusEvent> alarms)
    {
        if (BitIsSet(data[0], 5)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 4)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 3)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 2)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 1)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));
        if (BitIsSet(data[0], 0)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_NEAR_END"), string.Empty, Severity.Warning));

        if (BitIsSet(data[3], 5)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 4)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 3)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 2)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 1)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));
        if (BitIsSet(data[3], 0)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW1"), string.Empty, Severity.Warning));

        if (BitIsSet(data[4], 5)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 4)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 3)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 2)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 1)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));
        if (BitIsSet(data[4], 0)) alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CASSETTE_SKEW2"), string.Empty, Severity.Warning));

        if (data[6] == '1') alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "GATE1_DETECTED"), string.Empty, Severity.Warning));
        if (data[7] == '1') alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "GATE2_DETECTED"), string.Empty, Severity.Warning));
        if (data[9] == '1') alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "EXIT1_DETECTED"), string.Empty, Severity.Warning));
        if (data[10] == '1') alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "REJECT_IN_DETECTED"), string.Empty, Severity.Warning));
        if (data[11] == '1') alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "REJECT_BOX_UNLOCK"), string.Empty, Severity.Warning));
        if (data[12] == '1') alarms.Add(CreateAlarm(new ErrorCode("DEV", "HCDM", "STATUS", "CIS_OPEN"), string.Empty, Severity.Warning));
    }

    private static int EstimateTotalRequestedFromPayload(byte[] payload)
    {
        if (payload == null || payload.Length == 0) return 0;
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
        catch { return 0; }
    }
}
