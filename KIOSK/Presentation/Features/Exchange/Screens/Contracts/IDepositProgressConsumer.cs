using Kiosk.Application.Features.ExchangeV2.Services;

namespace Kiosk.ViewModels.Steps;

public interface IDepositProgressConsumer
{
    void ApplyDepositProgress(ExchangeDepositProgressChangedEventArgs progress);
}
