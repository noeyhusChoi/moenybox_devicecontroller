using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Features.Exchange.Pages.ViewModels.Popup;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels
{
    public partial class ExchangeV2IdScanConsentViewModel : StepViewModelBase
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
                _popup.ShowPopup<ExchangePopupTermsViewModel>();
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
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
