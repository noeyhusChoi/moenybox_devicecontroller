using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels;

public sealed partial class HomeLanguageSelectionViewModel : ObservableObject
{
    private readonly Action<HomeServiceType, string> _selectLanguageAction;

    public HomeLanguageSelectionViewModel(
        HomeServiceType serviceType,
        IReadOnlyCollection<string> supportedLanguageCodes,
        Action<HomeServiceType, string> selectLanguageAction)
    {
        ServiceType = serviceType;
        _selectLanguageAction = selectLanguageAction;

        KoreanLanguage = CreateLanguageOption("ko-KR", "한국어", "pack://application:,,,/Assets/Flag/KOR.png");
        EnglishLanguage = CreateLanguageOption("en-US", "English", "pack://application:,,,/Assets/Flag/USD.png");
        JapaneseLanguage = CreateLanguageOption("ja-JP", "日本語", "pack://application:,,,/Assets/Flag/JPY.png");
        SimplifiedChineseLanguage = CreateLanguageOption("zh-CN", "简体中文", "pack://application:,,,/Assets/Flag/CNY.png");
        TraditionalChineseLanguage = CreateLanguageOption("zh-TW", "繁體中文", "pack://application:,,,/Assets/Flag/TWD.png");

        ApplyVisibility(supportedLanguageCodes);
        SetInitialSelection();
    }

    public HomeServiceType ServiceType { get; }
    public HomeLanguageOptionViewModel KoreanLanguage { get; }
    public HomeLanguageOptionViewModel EnglishLanguage { get; }
    public HomeLanguageOptionViewModel JapaneseLanguage { get; }
    public HomeLanguageOptionViewModel SimplifiedChineseLanguage { get; }
    public HomeLanguageOptionViewModel TraditionalChineseLanguage { get; }

    public IReadOnlyList<HomeLanguageOptionViewModel> LanguageOptions =>
        [
            KoreanLanguage,
            EnglishLanguage,
            JapaneseLanguage,
            SimplifiedChineseLanguage,
            TraditionalChineseLanguage
        ];

    private HomeLanguageOptionViewModel CreateLanguageOption(string languageCode, string label, string assetPath)
    {
        return new HomeLanguageOptionViewModel(
            languageCode,
            label,
            assetPath,
            new RelayCommand(() => SelectLanguage(languageCode)));
    }

    private void ApplyVisibility(IReadOnlyCollection<string> supportedLanguageCodes)
    {
        var visibleCodes = new HashSet<string>(supportedLanguageCodes, StringComparer.OrdinalIgnoreCase);

        foreach (var option in LanguageOptions)
        {
            option.IsVisible = visibleCodes.Contains(option.LanguageCode);
        }
    }

    private void SetInitialSelection()
    {
        var initialLanguage = LanguageOptions.FirstOrDefault(option => option.IsVisible)?.LanguageCode;
        if (initialLanguage is not null)
            UpdateSelectedLanguage(initialLanguage);
    }

    private void SelectLanguage(string languageCode)
    {
        UpdateSelectedLanguage(languageCode);
        _selectLanguageAction(ServiceType, languageCode);
    }

    private void UpdateSelectedLanguage(string languageCode)
    {
        foreach (var option in LanguageOptions)
        {
            option.IsSelected = option.IsVisible && string.Equals(option.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase);
        }
    }
}
