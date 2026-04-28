using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Features.ExchangeV2.StateMachine;
using Kiosk.ViewModels.BottomActions;
using Kiosk.ViewModels.Steps;

namespace Kiosk.ViewModels;

public interface IExchangeScreenFactory
{
    ExchangeStepViewModelBase CreateStepViewModel(
        ExchangeStep step,
        ExchangeFlowContext context,
        Func<Task> showModalAsync);

    BottomActionViewModelBase? CreateBottomAction(
        ExchangeStep step,
        ExchangeFlowContext context,
        ExchangeStepViewModelBase? stepViewModel,
        IAsyncRelayCommand homeCommand);

    IReadOnlyList<ExchangeProgressStepViewModel> CreateProgressSteps(ExchangeStep step);

    bool ShouldShowStepHeader(ExchangeStep step);
    bool ShouldUseFeatureBackground(ExchangeStep step);
    bool ShouldCollapseShellChrome(ExchangeStep step);
}
