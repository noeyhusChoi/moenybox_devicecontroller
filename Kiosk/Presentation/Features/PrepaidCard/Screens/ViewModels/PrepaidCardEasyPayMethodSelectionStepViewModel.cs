using CommunityToolkit.Mvvm.Input;
using Kiosk.ViewModels.Steps;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardEasyPayMethodSelectionStepViewModel : ExchangeStepViewModelBase
{
    public PrepaidCardEasyPayMethodSelectionStepViewModel(
        IAsyncRelayCommand alipayCommand,
        IAsyncRelayCommand wechatPayCommand)
    {
        AlipayCommand = alipayCommand;
        WechatPayCommand = wechatPayCommand;
    }

    public IAsyncRelayCommand AlipayCommand { get; }

    public IAsyncRelayCommand WechatPayCommand { get; }
}
