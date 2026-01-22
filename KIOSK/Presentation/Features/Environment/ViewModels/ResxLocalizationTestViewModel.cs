using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Shared.Abstractions;
using Localization.Resx;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.Environment.ViewModels;

public sealed partial class ResxLocalizationTestViewModel : ObservableObject, INavigable
{
    private readonly IResxLocalizationService _localization;
    private readonly INavigationService _nav;

    [ObservableProperty]
    private string currentCultureName = string.Empty;

    public ResxLocalizationTestViewModel(
        IResxLocalizationService localization,
        INavigationService nav)
    {
        _localization = localization;
        _nav = nav;
        CurrentCultureName = _localization.CurrentCulture.Name;
    }

    public Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        _localization.LanguageChanged += OnLanguageChanged;
        CurrentCultureName = _localization.CurrentCulture.Name;
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync()
    {
        _localization.LanguageChanged -= OnLanguageChanged;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void SetCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return;

        var culture = new CultureInfo(cultureName);
        _localization.SetCulture(culture);
    }

    [RelayCommand]
    private async Task Back()
    {
        await _nav.NavigateTo<KIOSK.ViewModels.EnvironmentViewModel>();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        CurrentCultureName = _localization.CurrentCulture.Name;
    }
}
