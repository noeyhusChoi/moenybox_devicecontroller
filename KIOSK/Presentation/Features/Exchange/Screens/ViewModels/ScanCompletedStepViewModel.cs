namespace Kiosk.ViewModels.Steps;

public sealed class ScanCompletedStepViewModel : ExchangeStepViewModelBase
{
    public ScanCompletedStepViewModel(
        bool isSuccess,
        string? documentType,
        IReadOnlyDictionary<string, string>? fields,
        string? errorMessage = null,
        string? title = "")
    {
        Title = title;
        IsSuccess = isSuccess;
        Headline = isSuccess
            ? "신분증 스캔이 완료 되었습니다"
            : "신분증 스캔에 실패하였습니다";
        Description = isSuccess
            ? "신분증 및 여권을 반드시 회수하신 후\n다음 스텝을 진행해주세요"
            : "안내 영상을 자세히 확인하신 후\n안내에 따라 신분증을 다시 스캔해주세요";
        NoticeText = isSuccess
            ? "분실에 대한 책임은 당사가 지지 않습니다"
            : string.Empty;
        ErrorDetailText = isSuccess || string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : errorMessage;
        HasErrorDetail = !string.IsNullOrWhiteSpace(ErrorDetailText);
        Body = string.IsNullOrWhiteSpace(errorMessage)
            ? Description
            : errorMessage;
        DocumentType = string.IsNullOrWhiteSpace(documentType) ? "-" : documentType;
        Fields = isSuccess
            ? fields ?? new Dictionary<string, string>()
            : new Dictionary<string, string>();
    }

    public bool IsSuccess { get; }
    public string Headline { get; }
    public string Description { get; }
    public string NoticeText { get; }
    public string ErrorDetailText { get; }
    public bool HasErrorDetail { get; }
    public string DocumentType { get; }
    public IReadOnlyDictionary<string, string> Fields { get; }
}
