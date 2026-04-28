using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kiosk.ViewModels.Overlays;

public partial class TermsOverlayViewModel : ObservableObject
{
    public TermsOverlayViewModel(
        IRelayCommand closeCommand,
        string? title = "이용약관",
        string? body = "서비스 이용을 위해 본인 확인, 거래 처리, 법령상 의무 이행을 위한 정보가 수집 및 이용됩니다.\n\n자세한 내용은 안내 직원에게 문의하시거나 별도 고지된 약관 전문을 확인해 주세요.",
        string? confirmText = "확인")
    {
        Title = title;
        Body = body;
        ConfirmText = confirmText;
        CloseCommand = closeCommand;
    }

    public string? Title { get; }
    public string? Body { get; }
    public string? ConfirmText { get; }
    public IRelayCommand CloseCommand { get; }
}
