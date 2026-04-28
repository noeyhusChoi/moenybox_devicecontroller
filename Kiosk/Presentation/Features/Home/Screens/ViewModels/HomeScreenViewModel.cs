using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels;

public enum HomeServiceType
{
    TransportationCard,
    Exchange,
    TaxRefund
}

public sealed class HomeServiceCardViewModel
{
    public HomeServiceCardViewModel(
        HomeServiceType serviceType,
        string title,
        string subtitle,
        string description,
        string iconAssetPath,
        bool isEnabled,
        IAsyncRelayCommand command)
    {
        ServiceType = serviceType;
        Title = title;
        Subtitle = subtitle;
        Description = description;
        IconAssetPath = iconAssetPath;
        IsEnabled = isEnabled;
        Command = command;
    }

    public HomeServiceType ServiceType { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Description { get; }
    public string IconAssetPath { get; }
    public bool IsEnabled { get; }
    public IAsyncRelayCommand Command { get; }
}

public partial class HomeLanguageOptionViewModel : ObservableObject
{
    public HomeLanguageOptionViewModel(
        string languageCode,
        string label,
        string assetPath,
        IRelayCommand selectCommand)
    {
        LanguageCode = languageCode;
        Label = label;
        AssetPath = assetPath;
        SelectCommand = selectCommand;
    }

    public string LanguageCode { get; }
    public string Label { get; }
    public string AssetPath { get; }
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isVisible = true;
}

public sealed partial class HomeScreenViewModel : ObservableObject
{
    public HomeScreenViewModel(
        IAsyncRelayCommand requestTransportationCommand,
        IAsyncRelayCommand requestExchangeCommand,
        IAsyncRelayCommand requestTaxRefundCommand)
    {
        TransportationCard = new HomeServiceCardViewModel(
            HomeServiceType.TransportationCard,
            "교통선불카드",
            "Transportation prepaid card",
            "교통선불카드 서비스로 이동합니다.",
            "pack://application:,,,/Assets/Image/img_card.png",
            true,
            requestTransportationCommand);
        ExchangeCard = new HomeServiceCardViewModel(
            HomeServiceType.Exchange,
            "외화 판매",
            "Exchange",
            "외화를 원화로 환전하는 플로우로 이동합니다.",
            "pack://application:,,,/Assets/Image/img_exchange.png",
            true,
            requestExchangeCommand);
        TaxRefundCard = new HomeServiceCardViewModel(
            HomeServiceType.TaxRefund,
            "택스 리펀드",
            "Tax refund",
            "추후 연결될 택스 리펀드 서비스입니다.",
            "pack://application:,,,/Assets/Image/img_refund.png",
            true,
            requestTaxRefundCommand);
    }

    public string Title => "원하시는 서비스를 선택해주세요";
    public HomeServiceCardViewModel TransportationCard { get; }
    public HomeServiceCardViewModel ExchangeCard { get; }
    public HomeServiceCardViewModel TaxRefundCard { get; }
}
