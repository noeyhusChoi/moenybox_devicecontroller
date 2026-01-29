using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Presentation.Features.Exchange.Pages.ViewModels.Popup;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels
{
    public partial class ExchangeIDScanConsentViewModel : StepViewModelBase
    {

        private readonly IPopupService _popup;

        public ExchangeIDScanConsentViewModel(IPopupService popup)
        {
            _popup = popup;
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct) => Task.CompletedTask;

        public override Task OnUnloadAsync() => Task.CompletedTask;

        [RelayCommand]
        private async Task OpenTerms()
        {
            _popup.ShowPopup<ExchangePopupTermsViewModel>();
        }

        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);

    }
}
