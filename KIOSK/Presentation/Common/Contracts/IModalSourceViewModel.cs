using System.ComponentModel;

namespace Kiosk.ViewModels;

public interface IModalSourceViewModel : INotifyPropertyChanged
{
    object? CurrentModalViewModel { get; }
}
