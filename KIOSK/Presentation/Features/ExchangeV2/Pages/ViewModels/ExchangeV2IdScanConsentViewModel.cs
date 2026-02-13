using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Abstractions;
using KIOSK.Presentation.Features.Exchange.Popup.ViewModels;
using KIOSK.Presentation.Features.ExchangeV2.Popup.ViewModels;
using KIOSK.Presentation.Navigation.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels
{
    public partial class ExchangeV2IdScanConsentViewModel : PageViewModelBase
    {
        [ObservableProperty]
        private bool isTermsChecked;

        private readonly IPopupService _popup;

        public ExchangeV2IdScanConsentViewModel(IPopupService popup)
        {
            _popup = popup;
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct) => Task.CompletedTask;

        public override Task OnUnloadAsync() => Task.CompletedTask;

        [RelayCommand]
        private async Task Terms()
        {
            try
            {
                _popup.ShowPopup<ExchangeV2TermsPopupViewModel>();
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }

        #region Commands

        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);

        #endregion
    }
}
