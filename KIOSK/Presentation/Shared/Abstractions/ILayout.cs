namespace KIOSK.Presentation.Shared.Abstractions
{
    public interface ILayout : INavigable, IPopup
    {
        object? CurrentPage { get; set; }
    }
}
