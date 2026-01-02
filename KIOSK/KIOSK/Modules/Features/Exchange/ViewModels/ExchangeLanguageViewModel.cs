using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.FSM;
using KIOSK.Models;
using KIOSK.Services;
using Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KIOSK.ViewModels;

public partial class ExchangeLanguageViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
{
    public Func<Task>? OnStepMain { get; set; }
    public Func<Task>? OnStepPrevious { get; set; }
    public Func<string?, Task>? OnStepNext { get; set; }
    public Action<Exception>? OnStepError { get; set; }

    private readonly IServiceProvider _provider;

    public ExchangeLanguageViewModel(IServiceProvider provider)
    {
        _provider = provider;
    }

    public async Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        // TODO: 로딩 시 필요한 작업 수행
    }

    public async Task OnUnloadAsync()
    {
        // TODO: 언로드 시 필요한 작업 수행
    }

    #region Commands
    [RelayCommand]
    private async Task Main()
    {
        try
        {
            if (OnStepMain is not null)
                await OnStepMain();
        }
        catch (Exception ex)
        {
            if (OnStepError is not null)
                OnStepError(ex);
        }
    }

    [RelayCommand]
    private async Task Previous()
    {
        try
        {
            if (OnStepPrevious is not null)
                await OnStepPrevious();
        }
        catch (Exception ex)
        {
            if (OnStepError is not null)
                OnStepError(ex);
        }
    }

    [RelayCommand]
    private async Task Next(object? parameter)
    {
        var lang = _provider.GetRequiredService<ILocalizationService>();
        if (parameter is string param)
        {
            try
            {
                CultureInfo culture = new CultureInfo("ko-KR");
                switch (param)
                {
                    case "1":
                        culture = new CultureInfo("en-US");
                        break;
                    case "2":
                        culture = new CultureInfo("zh-CN");
                        break;
                    case "3":
                        culture = new CultureInfo("zh-TW");
                        break;
                    case "4":
                        culture = new CultureInfo("ja-JP");
                        break;
                    case "5":
                        culture = new CultureInfo("ko-KR");
                        break;
                }
                lang.SetCulture(culture);

                if (OnStepNext is not null)
                    await OnStepNext("");
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }
    }
    #endregion
}