using System.Threading;

namespace Kiosk.Application.Services.Time;

public sealed class ClockService : IClockService, IDisposable
{
    private readonly Timer _timer;

    public ClockService()
    {
        _timer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public DateTime Now => DateTime.Now;

    public event EventHandler<DateTime>? TimeChanged;

    public void Dispose()
    {
        _timer.Dispose();
    }

    private void OnTimerTick(object? state)
    {
        TimeChanged?.Invoke(this, DateTime.Now);
    }
}
