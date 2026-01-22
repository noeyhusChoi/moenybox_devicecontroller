using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Exchange;
using KIOSK.FSM;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.ViewModels;

public partial class ExchangeLanguageViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
{
    public Func<Task>? OnStepMain { get; set; }
    public Func<Task>? OnStepPrevious { get; set; }
    public Func<string?, Task>? OnStepNext { get; set; }
    public Action<Exception>? OnStepError { get; set; }

    private readonly IExchangeSelectLanguageUseCase _selectLanguageUseCase;

    public ExchangeLanguageViewModel(IExchangeSelectLanguageUseCase selectLanguageUseCase)
    {
        _selectLanguageUseCase = selectLanguageUseCase;
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
            OnStepError?.Invoke(ex);
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
            OnStepError?.Invoke(ex);
        }
    }

    [RelayCommand]
    private async Task Next(object? parameter)
    {
        if (parameter is string param)
        {
            try
            {
                await _selectLanguageUseCase.SelectAsync(param);

                if (OnStepNext is not null)
                    await OnStepNext("");
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
    }
    #endregion
}
