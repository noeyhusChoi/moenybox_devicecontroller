namespace KIOSK.Presentation.Abstractions
{
    public interface ILayout : IViewLifecycle, IPopup
    {
        object? CurrentPage { get; set; }
    }
}
