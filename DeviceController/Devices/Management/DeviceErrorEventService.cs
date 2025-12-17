using System.Diagnostics;
using KIOSK.Device.Abstractions;

namespace KIOSK.Devices.Management;

/// <summary>
/// 상태 업데이트 기반으로 필요한 부가 이벤트를 파생시키는 훅.
/// (현재는 최소 구현: 알람이 있으면 Trace에 기록)
/// </summary>
public sealed class DeviceErrorEventService
{
    public Task OnStatusUpdated(string name, DeviceStatusSnapshot snapshot)
    {
        try
        {
            if (snapshot.Alarms is { Count: > 0 })
                Trace.WriteLine($"[DeviceErrorEvent] {name} alarms: {snapshot.Alarms.Count}");
        }
        catch
        {
        }

        return Task.CompletedTask;
    }
}

