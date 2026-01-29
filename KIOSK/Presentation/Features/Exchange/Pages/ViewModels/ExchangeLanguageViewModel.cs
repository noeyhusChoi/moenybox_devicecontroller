using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Exchange;
using KIOSK.FSM;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels;

public partial class ExchangeLanguageViewModel : StepViewModelBase
{

    private readonly IExchangeSelectLanguageUseCase _selectLanguageUseCase;

    public ExchangeLanguageViewModel(IExchangeSelectLanguageUseCase selectLanguageUseCase)
    {
        _selectLanguageUseCase = selectLanguageUseCase;
    }

    public override Task OnLoadAsync(object? parameter, CancellationToken ct) => Task.CompletedTask;

    public override Task OnUnloadAsync() => Task.CompletedTask;

    #region Commands
    [RelayCommand]
    private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

    [RelayCommand]
    private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

    [RelayCommand]
    private async Task Next(object? parameter)
    {
        if (parameter is not string param)
        {
            return;
        }

        try
        {
            await _selectLanguageUseCase.SelectAsync(param);
            await ExecuteStepAsync(OnStepNext, param);
        }
        catch (Exception ex)
        {
            OnStepError?.Invoke(ex);
        }
    }
    #endregion
}
