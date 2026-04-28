using CommunityToolkit.Mvvm.ComponentModel;
using Kiosk.Application.Services.Devices.IdScanner;

namespace Kiosk.ViewModels.Steps;

public partial class ScanningStepViewModel : ExchangeStepViewModelBase, IScannerEventConsumer
{
    public ScanningStepViewModel(
        string? body = "문서를 제거하지 말고 잠시 기다려 주세요.",
        string? title = "신분증 스캔")
    {
        Title = title;
        Body = body;
        Presence = "Waiting";
        ProgressMessage = "스캐너를 준비하고 있습니다.";
    }

    [ObservableProperty]
    private string? presence;

    [ObservableProperty]
    private string? progressMessage;

    [ObservableProperty]
    private bool isFaulted;

    public void ApplyScannerEvent(IdScannerEvent e)
    {
        switch (e)
        {
            case IdDocumentDetectedEvent:
                ProgressMessage = "Document detected. Hold it steady.";
                break;
            case IdScanStatusChangedEvent status:
                Presence = status.Phase.ToString();
                ProgressMessage = status.Phase switch
                {
                    IdScannerScanPhase.WaitingForDocument => "Waiting for the document.",
                    IdScannerScanPhase.Scanning => "Scanner is reading the document.",
                    IdScannerScanPhase.ScanComplete => "Scan complete. Running OCR.",
                    IdScannerScanPhase.Timeout => "Scanner timed out while waiting for removal.",
                    IdScannerScanPhase.Removed => "Document removed.",
                    _ => "Scanner fault detected."
                };
                IsFaulted = status.Phase == IdScannerScanPhase.Faulted;
                break;
            case IdScannerFaultedEvent fault:
                Presence = "Faulted";
                ProgressMessage = fault.Message;
                IsFaulted = true;
                break;
        }
    }
}
