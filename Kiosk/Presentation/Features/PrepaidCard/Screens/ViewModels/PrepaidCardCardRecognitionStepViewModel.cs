using Kiosk.ViewModels.Steps;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardCardRecognitionStepViewModel : ExchangeStepViewModelBase
{
    public PrepaidCardCardRecognitionStepViewModel(string titleText, string previewImageSource)
    {
        TitleText = titleText;
        PreviewImageSource = previewImageSource;
    }

    public string TitleText { get; }

    public string PreviewImageSource { get; }
}
