using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.ViewModels.Exchange.Popup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Localization;
using System.Threading.Tasks;
using KIOSK.Infrastructure.UI;
using KIOSK.Infrastructure.UI.Navigation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace KIOSK.ViewModels
{
    public partial class ExchangeIDScanConsentViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError
    {
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        private readonly IPopupService _popup;

        public ExchangeIDScanConsentViewModel(IPopupService popup)
        {
            _popup = popup;
        }

        [RelayCommand]
        private async Task OpenTerms()
        {
            _popup.ShowLocal<ExchangePopupTermsViewModel>();
        }

        [RelayCommand]
        private async Task Main()
        {
            try
            {
                OnStepMain?.Invoke();
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
                OnStepPrevious?.Invoke();
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }

        [RelayCommand]
        private async Task Next()
        {
            try
            {
                OnStepNext?.Invoke("");
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }

    }
}
