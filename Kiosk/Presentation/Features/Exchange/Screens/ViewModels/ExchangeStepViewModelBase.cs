using CommunityToolkit.Mvvm.ComponentModel;

namespace Kiosk.ViewModels.Steps;

public abstract partial class ExchangeStepViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string? title;

    [ObservableProperty]
    private string? body;
}
