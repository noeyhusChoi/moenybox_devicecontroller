namespace Kiosk.ViewModels.Steps;

public sealed class MessageStepViewModel : ExchangeStepViewModelBase
{
    public MessageStepViewModel(string? title, string? body)
    {
        Title = title;
        Body = body;
    }
}
