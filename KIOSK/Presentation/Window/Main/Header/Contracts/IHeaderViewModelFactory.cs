namespace Kiosk.ViewModels;

public interface IHeaderViewModelFactory
{
    string GetLogoAssetPath();
    HeaderViewModel CreateHomeHeader();
    HeaderViewModel CreateExchangeHeader(string? timerText);
}
