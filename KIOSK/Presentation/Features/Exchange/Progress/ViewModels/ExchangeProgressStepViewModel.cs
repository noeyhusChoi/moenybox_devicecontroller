using CommunityToolkit.Mvvm.ComponentModel;

namespace Kiosk.ViewModels;

public partial class ExchangeProgressStepViewModel : ObservableObject
{
    public ExchangeProgressStepViewModel(string numberText, string label)
    {
        NumberText = numberText;
        Label = label;
    }

    public string NumberText { get; }
    public string Label { get; }

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private bool isComplete;
}
