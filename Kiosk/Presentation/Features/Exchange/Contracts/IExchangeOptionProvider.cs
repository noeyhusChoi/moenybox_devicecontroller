using Kiosk.Application.Features.ExchangeV2.StateMachine;
using Kiosk.ViewModels.Steps;

namespace Kiosk.ViewModels;

public interface IExchangeOptionProvider
{
    IReadOnlyList<CurrencyOptionViewModel> CreateCurrencyOptions();
}
