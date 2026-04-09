using Kiosk.Application.Services.Resx;
using Kiosk.ViewModels;
using System.Globalization;

namespace Kiosk.Tests.Unit.Presentation.Home;

public sealed class HomeShellViewModelTests
{
    [Fact]
    public async Task ExchangeSelection_ShowsLanguageScreen_AndRaisesEntryRequested()
    {
        var appCulture = new TestAppCulture("ko-KR");
        var sut = new HomeShellViewModel(appCulture);
        HomeServiceEntryRequestedEventArgs? raisedEvent = null;
        sut.ServiceEntryRequested += (_, args) => raisedEvent = args;

        await sut.HomeScreen.ExchangeCard.Command.ExecuteAsync(null);

        sut.CurrentScreenViewModel.Should().BeOfType<HomeLanguageSelectionViewModel>();
        var languageSelection = sut.CurrentScreenViewModel.Should().BeOfType<HomeLanguageSelectionViewModel>().Subject;
        languageSelection.KoreanLanguage.IsVisible.Should().BeTrue();
        languageSelection.EnglishLanguage.IsVisible.Should().BeTrue();
        languageSelection.TraditionalChineseLanguage.IsVisible.Should().BeTrue();

        languageSelection.EnglishLanguage.SelectCommand.Execute(null);

        raisedEvent.Should().NotBeNull();
        raisedEvent!.ServiceType.Should().Be(HomeServiceType.Exchange);
        raisedEvent.LanguageCode.Should().Be("en-US");
        appCulture.CurrentCulture.Name.Should().Be("en-US");
    }

    [Fact]
    public async Task ResetToServiceSelection_RestoresHomeScreenAndDefaultCulture()
    {
        var appCulture = new TestAppCulture("ja-JP");
        var sut = new HomeShellViewModel(appCulture);

        await sut.HomeScreen.ExchangeCard.Command.ExecuteAsync(null);
        sut.CurrentScreenViewModel.Should().BeOfType<HomeLanguageSelectionViewModel>();

        sut.ResetToServiceSelection();

        sut.CurrentScreenViewModel.Should().BeSameAs(sut.HomeScreen);
        appCulture.CurrentCulture.Name.Should().Be("ko-KR");
    }

    private sealed class TestAppCulture : IAppCulture
    {
        private readonly List<CultureInfo> _supportedCultures =
        [
            CultureInfo.GetCultureInfo("ko-KR"),
            CultureInfo.GetCultureInfo("en-US"),
            CultureInfo.GetCultureInfo("ja-JP"),
            CultureInfo.GetCultureInfo("zh-CN"),
            CultureInfo.GetCultureInfo("zh-TW")
        ];

        public TestAppCulture(string initialCulture)
        {
            CurrentCulture = CultureInfo.GetCultureInfo(initialCulture);
        }

        public CultureInfo CurrentCulture { get; private set; }

        public IReadOnlyList<CultureInfo> SupportedCultures => _supportedCultures;

        public event EventHandler? CultureChanged;

        public void SetCulture(CultureInfo culture)
        {
            CurrentCulture = culture;
            CultureChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
