using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows.Threading;

public interface IInactivityService : INotifyPropertyChanged
{
    void Start(TimeSpan timeout, Action onTimeout);
    void Reset();
    void Stop();
    int RemainingSeconds { get; }
}

public partial class InactivityService : ObservableObject, IInactivityService
{
    private readonly DispatcherTimer _timer;
    private DateTime _lastReset;
    private TimeSpan _timeout;

    [ObservableProperty]
    private int remainingSeconds;

    private Action? _onTimeout;

    public InactivityService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        _timer.Tick += (s, e) =>
        {
            var remain = _timeout - (DateTime.Now - _lastReset);

            RemainingSeconds = (int)Math.Ceiling(remain.TotalSeconds);
            if (RemainingSeconds <= 0)
            {
                Stop();
                _onTimeout?.Invoke();
            }
        };
    }

    public void Start(TimeSpan timeout, Action onTimeout)
    {
        _timeout = timeout;
        _onTimeout = onTimeout;
        _lastReset = DateTime.Now;
        RemainingSeconds = (int)timeout.TotalSeconds;
        _timer.Start();
    }

    public void Reset()
    {

        _lastReset = DateTime.Now;
        RemainingSeconds = (int)_timeout.TotalSeconds;

    }

    public void Stop()
    {
        _timer.Stop();
        _timeout = TimeSpan.Zero;
        RemainingSeconds = 0;
    }
}
