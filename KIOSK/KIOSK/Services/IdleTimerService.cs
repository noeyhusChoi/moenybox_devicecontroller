using System.Windows.Threading;

namespace KIOSK.Services;

public class IdleTimerService
{
    public event Action OnAlmostIdle;   // 예: 30초 전 경고용
    public event Action OnIdleTimeout;  // 복귀용

    private readonly TimeSpan _idleTime = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _alertTime = TimeSpan.FromSeconds(30);
    private DispatcherTimer _mainTimer, _alertTimer;

    public IdleTimerService()
    {
        _mainTimer = new DispatcherTimer { Interval = _idleTime };
        _mainTimer.Tick += (s, e) => {
            _mainTimer.Stop();
            OnIdleTimeout?.Invoke();
        };

        _alertTimer = new DispatcherTimer { Interval = _idleTime - _alertTime };
        _alertTimer.Tick += (s, e) => {
            _alertTimer.Stop();
            OnAlmostIdle?.Invoke();
        };
    }

    public void Start() { _mainTimer.Start(); _alertTimer.Start(); }
    public void Reset() { _mainTimer.Stop(); _alertTimer.Stop(); Start(); }
    public void Stop() { _mainTimer.Stop(); _alertTimer.Stop(); }
}
