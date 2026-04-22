using Kiosk.Application.Services.Devices.IdScanner;

namespace Kiosk.ViewModels.Steps;

public interface IScannerEventConsumer
{
    void ApplyScannerEvent(IdScannerEvent e);
}
