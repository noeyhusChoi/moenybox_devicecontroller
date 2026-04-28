using CommunityToolkit.Mvvm.Input;
using Kiosk.ViewModels.Steps;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardPaymentMethodSelectionStepViewModel : ExchangeStepViewModelBase
{
    public PrepaidCardPaymentMethodSelectionStepViewModel(
        IAsyncRelayCommand cashCommand,
        IAsyncRelayCommand alipayCommand,
        IAsyncRelayCommand wechatPayCommand)
    {
        CashCommand = cashCommand;
        AlipayCommand = alipayCommand;
        WechatPayCommand = wechatPayCommand;
    }

    public IAsyncRelayCommand CashCommand { get; }

    public IAsyncRelayCommand AlipayCommand { get; }

    public IAsyncRelayCommand WechatPayCommand { get; }
}
