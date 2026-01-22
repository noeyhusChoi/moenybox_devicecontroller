using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Navigation.Popup;
using KIOSK.Presentation.Shared.Abstractions;
using KIOSK.ViewModels.Exchange.Popup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace KIOSK.Presentation.Features.ExchangeV2.ViewModels
{
    public partial class ExchangeV2IdScanConsentViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        [ObservableProperty]
        private bool isTermsChecked;

        private readonly IPopupService _popup;

        public ExchangeV2IdScanConsentViewModel(IPopupService popup)
        {
            _popup = popup;
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            // TODO: 로딩 시 필요한 작업 수행
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }

        [RelayCommand]
        private async Task Terms()
        {
            try
            {
                _popup.ShowLocal<ExchangePopupTermsViewModel>();
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
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
}
