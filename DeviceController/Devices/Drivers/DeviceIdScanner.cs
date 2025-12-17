using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers;

/// <summary>
/// 신분증 스캐너 드라이버 (스텁).
/// - 실제 PR22 SDK가 없는 환경에서도 빌드 가능하도록 최소 구현만 제공.
/// </summary>
public sealed class DeviceIdScanner : DeviceBase
{
    private int _failThreshold;

    public event EventHandler<(int page, string light, string path)>? ImageSaved;
    public event EventHandler<ScanEvent>? ScanSequence;

    public enum ScanEvent
    {
        Empty,
        Scanning,
        ScanComplete,
        Removed,
        RemovalTimeout
    }

    public DeviceIdScanner(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
    }

    public override Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        _failThreshold = 0;
        return Task.FromResult(CreateSnapshot(new[]
        {
            CreateAlarm("IDSCANNER", "SDK 미적용(스텁)", Severity.Warning)
        }));
    }

    public override Task<DeviceStatusSnapshot> GetStatusAsync(CancellationToken ct = default, string snapshotId = "")
    {
        var alarms = new List<DeviceAlarm>();
        if (_failThreshold > 0)
            alarms.Add(CreateAlarm("IDSCANNER", "SDK 미적용(스텁)", Severity.Warning));

        return Task.FromResult(CreateSnapshot(alarms));
    }

    public override Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        => Task.FromResult(new CommandResult(false, $"[{command.Name}] NOT SUPPORTED (stub)"));
}

