using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers;

/// <summary>
/// 지폐 투입기 드라이버 (스텁).
/// - 실제 장비 SDK(MPOST 등)가 없는 환경에서도 빌드 가능하도록 최소 구현만 제공.
/// </summary>
public sealed class DeviceDeposit : DeviceBase
{
    private int _failThreshold;

    public event EventHandler<string>? OnEscrowed;

    public DeviceDeposit(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
    }

    public override Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        _failThreshold = 0;
        return Task.FromResult(CreateSnapshot(new[]
        {
            CreateAlarm("DEPOSIT", "SDK 미적용(스텁)", Severity.Warning)
        }));
    }

    public override Task<DeviceStatusSnapshot> GetStatusAsync(CancellationToken ct = default, string snapshotId = "")
    {
        var alarms = new List<DeviceAlarm>();
        if (_failThreshold > 0)
            alarms.Add(CreateAlarm("DEPOSIT", "SDK 미적용(스텁)", Severity.Warning));

        return Task.FromResult(CreateSnapshot(alarms));
    }

    public override Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        => Task.FromResult(new CommandResult(false, $"[{command.Name}] NOT SUPPORTED (stub)"));
}

