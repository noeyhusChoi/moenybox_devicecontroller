namespace KIOSK.Presentation.Shared.Abstractions
{
    public interface ILayout : INavigable, IPopup
    {
        object? CurrentView { get; set; }
    }
}
