using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public partial class ConsentStepViewModel : ExchangeStepViewModelBase, ITermsAgreementStepViewModel
{
    public ConsentStepViewModel(
        IAsyncRelayCommand viewTermsCommand,
        string? title = "신분증을 준비해 주세요",
        string? body = "이용약관에 동의하셔야 다음 단계로 이동이 가능합니다.")
    {
        Title = title;
        Body = body;
        ViewTermsCommand = viewTermsCommand;
    }

    [ObservableProperty]
    private bool isAgreed;

    partial void OnIsAgreedChanged(bool value)
        => IsPrimaryEnabled = value;

    public string SupportedDocumentTypes { get; } = "여권, 주민등록증, 운전면허증, 외국인등록증";
    public string TermsTitle { get; } = "개인정보 수집 이용 및 제3자 제공 동의";
    public string TermsLinkText { get; } = "이용약관 보기";
    public IAsyncRelayCommand ViewTermsCommand { get; }
}
