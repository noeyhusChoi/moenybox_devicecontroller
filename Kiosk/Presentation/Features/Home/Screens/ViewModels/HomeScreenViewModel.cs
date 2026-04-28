using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Services.Resx;
using System.Globalization;

namespace Kiosk.ViewModels;

public sealed class HomeServiceCardViewModel
{
    public HomeServiceCardViewModel(
        string title,
        string subtitle,
        string description,
        string iconAssetPath,
        bool isEnabled,
        IAsyncRelayCommand command)
    {
        Title = title;
        Subtitle = subtitle;
        Description = description;
        IconAssetPath = iconAssetPath;
        IsEnabled = isEnabled;
        Command = command;
    }

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
}

public partial class HomeScreenViewModel : ObservableObject
{
    private readonly IAppCulture _appCulture;

    public HomeScreenViewModel(
        IAsyncRelayCommand requestExchangeCommand,
        IAppCulture appCulture)
    {
        _appCulture = appCulture;

        KoreanLanguage = new HomeLanguageOptionViewModel(
            "ko-KR",
            "한국어",
            "pack://application:,,,/Assets/Flag/KOR.png",
            new RelayCommand(() => SetSelectedLanguage("ko-KR")));
        EnglishLanguage = new HomeLanguageOptionViewModel(
            "en-US",
            "English",
            "pack://application:,,,/Assets/Flag/USD.png",
            new RelayCommand(() => SetSelectedLanguage("en-US")));
        JapaneseLanguage = new HomeLanguageOptionViewModel(
            "ja-JP",
            "日本語",
            "pack://application:,,,/Assets/Flag/JPY.png",
            new RelayCommand(() => SetSelectedLanguage("ja-JP")));
        TraditionalChineseLanguage = new HomeLanguageOptionViewModel(
            "zh-TW",
            "繁體中文",
            "pack://application:,,,/Assets/Flag/TWD.png",
            new RelayCommand(() => SetSelectedLanguage("zh-TW")));
        SimplifiedChineseLanguage = new HomeLanguageOptionViewModel(
            "zh-CN",
            "简体中文",
            "pack://application:,,,/Assets/Flag/CNY.png",
            new RelayCommand(() => SetSelectedLanguage("zh-CN")));

        TransportationCard = new HomeServiceCardViewModel(
            "교통선불카드",
            "Transportation prepaid card",
            "교통선불카드 서비스로 이동합니다.",
            "pack://application:,,,/Assets/Image/Card.png",
            false,
            new AsyncRelayCommand(() => Task.CompletedTask));
        ExchangeCard = new HomeServiceCardViewModel(
            "외화 판매",
            "Exchange",
            "외화를 원화로 환전하는 플로우로 이동합니다.",
            "pack://application:,,,/Assets/Image/Exchange.png",
            true,
            requestExchangeCommand);
        TaxRefundCard = new HomeServiceCardViewModel(
            "택스 리펀드",
            "Tax refund",
            "추후 연결될 택스 리펀드 서비스입니다.",
            "pack://application:,,,/Assets/Image/Refund.png",
            false,
            new AsyncRelayCommand(() => Task.CompletedTask));

        SetSelectedLanguage("ko-KR");
    }

    public string Title => "원하시는 서비스를 선택해주세요";
    public string Subtitle => string.Empty;
    public string CurrentDateText => DateTime.Now.ToString("yyyy.MM.dd");
    public string CurrentTimeText => DateTime.Now.ToString("HH:mm");
    public HomeServiceCardViewModel TransportationCard { get; }
    public HomeServiceCardViewModel ExchangeCard { get; }
    public HomeServiceCardViewModel TaxRefundCard { get; }
    public HomeLanguageOptionViewModel KoreanLanguage { get; }
    public HomeLanguageOptionViewModel EnglishLanguage { get; }
    public HomeLanguageOptionViewModel JapaneseLanguage { get; }
    public HomeLanguageOptionViewModel TraditionalChineseLanguage { get; }
    public HomeLanguageOptionViewModel SimplifiedChineseLanguage { get; }

    [ObservableProperty]
    private string selectedLanguageCode = "ko-KR";

    public void SetSelectedLanguage(string languageCode)
    {
        SelectedLanguageCode = languageCode;
        _appCulture.SetCulture(CultureInfo.GetCultureInfo(languageCode));

        KoreanLanguage.IsSelected = KoreanLanguage.LanguageCode == languageCode;
        EnglishLanguage.IsSelected = EnglishLanguage.LanguageCode == languageCode;
        JapaneseLanguage.IsSelected = JapaneseLanguage.LanguageCode == languageCode;
        TraditionalChineseLanguage.IsSelected = TraditionalChineseLanguage.LanguageCode == languageCode;
        SimplifiedChineseLanguage.IsSelected = SimplifiedChineseLanguage.LanguageCode == languageCode;
    }
}
