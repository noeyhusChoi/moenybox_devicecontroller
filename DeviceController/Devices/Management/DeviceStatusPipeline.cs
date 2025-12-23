using KIOSK.Device.Abstractions;

namespace KIOSK.Devices.Management;

public interface IDeviceStatusPipeline
{
    void Process(string name, DeviceStatusSnapshot snapshot);
}

/// <summary>
/// 상태 스냅샷 처리 파이프라인(필터/중복제거/정책 확장 포인트).
/// 현재는 Store로 전달만 수행한다.
/// </summary>
public sealed class DeviceStatusPipeline : IDeviceStatusPipeline
{
    private readonly IDeviceStatusStore _store;

    public DeviceStatusPipeline(IDeviceStatusStore store)
    {
        _store = store;
    }

    public void Process(string name, DeviceStatusSnapshot snapshot)
    {
        _store.Update(name, snapshot);
    }
}
