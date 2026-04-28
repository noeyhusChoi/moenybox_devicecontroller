using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.BottomActions;

public abstract class BottomActionViewModelBase : ObservableObject;

public sealed class BackOnlyActionViewModel : BottomActionViewModelBase
{
    public BackOnlyActionViewModel(
        IAsyncRelayCommand backCommand,
        bool isBackEnabled,
        string backText = "이전")
    {
        BackCommand = backCommand;
        IsBackEnabled = isBackEnabled;
        BackText = backText;
    }

    public string BackText { get; }
    public IAsyncRelayCommand BackCommand { get; }
    public bool IsBackEnabled { get; }
}

public sealed partial class BackAndPrimaryActionViewModel : BottomActionViewModelBase
{
    public BackAndPrimaryActionViewModel(
        IAsyncRelayCommand backCommand,
        bool isBackEnabled,
        IAsyncRelayCommand primaryCommand,
        string primaryText,
        bool isPrimaryEnabled,
        string backText = "이전")
    {
        BackCommand = backCommand;
        IsBackEnabled = isBackEnabled;
        PrimaryCommand = primaryCommand;
        PrimaryText = primaryText;
        IsPrimaryEnabled = isPrimaryEnabled;
        BackText = backText;
    }

    public string BackText { get; }
    public IAsyncRelayCommand BackCommand { get; }
    public bool IsBackEnabled { get; }
    public IAsyncRelayCommand PrimaryCommand { get; }

    [ObservableProperty]
    private string primaryText;

    [ObservableProperty]
    private bool isPrimaryEnabled;
}

public sealed class PrimaryOnlyActionViewModel : BottomActionViewModelBase
{
    public PrimaryOnlyActionViewModel(
        IAsyncRelayCommand primaryCommand,
        string primaryText,
        bool isPrimaryEnabled = true)
    {
        PrimaryCommand = primaryCommand;
        PrimaryText = primaryText;
        IsPrimaryEnabled = isPrimaryEnabled;
    }

    public IAsyncRelayCommand PrimaryCommand { get; }
    public string PrimaryText { get; }
    public bool IsPrimaryEnabled { get; }
}
