namespace Kiosk.Application.Services.Time;

public interface IClockService
{
    DateTime Now { get; }
    event EventHandler<DateTime>? TimeChanged;
}
