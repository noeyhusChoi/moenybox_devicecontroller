using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Health;

namespace KIOSK.Infrastructure.Devices.Status;

public interface IStatusPipeline
{
    void Process(string name, StatusSnapshot snapshot);
}

/// <summary>
/// 디바이스 상태 어댑터.
/// 실제 상태 처리(정규화/중복제거/저장/알림)는 HealthPipeline에서 수행한다.
/// </summary>
public sealed class StatusPipeline : IStatusPipeline
{
    private readonly IHealthPipeline _healthPipeline;

    public StatusPipeline(IHealthPipeline healthPipeline)
    {
        _healthPipeline = healthPipeline;
    }

    public void Process(string name, StatusSnapshot snapshot)
    {
        _healthPipeline.Process(HealthSignal.FromDevice(name, snapshot));
    }
}
