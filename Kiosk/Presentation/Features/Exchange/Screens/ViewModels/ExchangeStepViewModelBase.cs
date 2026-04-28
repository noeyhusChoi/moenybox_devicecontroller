using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public abstract partial class ExchangeStepViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string? title;

    [ObservableProperty]
    private string? body;

    [ObservableProperty]
    private IAsyncRelayCommand? secondaryCommand;

    [ObservableProperty]
    private bool isSecondaryEnabled = true;

    [ObservableProperty]
    private IAsyncRelayCommand? primaryCommand;

    [ObservableProperty]
    private bool isPrimaryEnabled = true;

    public bool HasSecondaryAction => SecondaryCommand is not null;
    public bool HasPrimaryAction => PrimaryCommand is not null;

    partial void OnSecondaryCommandChanged(IAsyncRelayCommand? value)
        => OnPropertyChanged(nameof(HasSecondaryAction));

    partial void OnPrimaryCommandChanged(IAsyncRelayCommand? value)
        => OnPropertyChanged(nameof(HasPrimaryAction));
}
