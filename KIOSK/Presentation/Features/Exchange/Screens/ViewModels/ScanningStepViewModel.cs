using CommunityToolkit.Mvvm.ComponentModel;
using Kiosk.Application.Services.Devices.IdScanner;

namespace Kiosk.ViewModels.Steps;

public partial class ScanningStepViewModel : ExchangeStepViewModelBase, IScannerEventConsumer
{
    public ScanningStepViewModel(
        string? body = "얼굴 사진이 있는 면을 아래로 하여 스캐너에 올려주세요.",
        string? title = "신분증 스캔을 진행해 주세요")
    {
        Title = title;
        Body = body;
        Presence = "WaitingForDocument";
        ProgressMessage = "스캐너를 준비하고 있습니다.";
        UpdateDebugStatusText();
    }

    [ObservableProperty]
    private string? presence;

    [ObservableProperty]
    private string? progressMessage;

    [ObservableProperty]
    private bool isFaulted;

    [ObservableProperty]
    private string debugStatusText = string.Empty;

    public string SupportedDocumentTypes { get; } = "여권, 주민등록증, 운전면허증, 외국인등록증";

    public void ApplyScannerEvent(IdScannerEvent e)
    {
        switch (e)
        {
            case IdDocumentDetectedEvent:
                ProgressMessage = "신분증이 감지되었습니다. 움직이지 말고 잠시 기다려주세요.";
                break;
            case IdScanStatusChangedEvent status:
                Presence = status.Phase.ToString();
                ProgressMessage = status.Phase switch
                {
                    IdScannerScanPhase.WaitingForDocument => "신분증을 올려주세요.",
                    IdScannerScanPhase.Scanning => "신분증을 스캔하고 있습니다.",
                    IdScannerScanPhase.ScanComplete => "스캔이 완료되었습니다. 정보를 확인하고 있습니다.",
                    IdScannerScanPhase.Timeout => "신분증 인식 대기 시간이 초과되었습니다.",
                    IdScannerScanPhase.Removed => "신분증이 제거되었습니다.",
                    _ => "스캐너 상태를 확인하고 있습니다."
                };
                IsFaulted = status.Phase == IdScannerScanPhase.Faulted;
                break;
            case IdScannerFaultedEvent fault:
                Presence = "Faulted";
                ProgressMessage = fault.Message;
                IsFaulted = true;
                break;
        }

        UpdateDebugStatusText();
    }

    private void UpdateDebugStatusText()
    {
        var phase = string.IsNullOrWhiteSpace(Presence) ? "-" : Presence;
        var message = string.IsNullOrWhiteSpace(ProgressMessage) ? "-" : ProgressMessage;
        DebugStatusText = $"[Debug] {phase} | {message}";
    }
}
