using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kiosk;

public sealed class KeyboardNavigationState : INotifyPropertyChanged
{
    public static KeyboardNavigationState Instance { get; } = new();

    private bool _isEnabled = true;

    private KeyboardNavigationState()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;

            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
