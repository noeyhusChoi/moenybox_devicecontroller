using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Core;
using KIOSK.Devices.Management;
using KIOSK.Models;
using KIOSK.Services;
using KIOSK.Services.API;
using KIOSK.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Modules.GTF.ViewModels
{
    public partial class GtfRefundSignatureViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError
    {
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfTaxRefundModel Current => _gtfTaxRefundService.Current;

        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        public GtfRefundSignatureViewModel(GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
        }


        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            // TODO: 로딩 시 필요한 작업 수행
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
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
                if (OnStepError is not null)
                    OnStepError(ex);
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
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }


        [RelayCommand]
        private async Task Next(object? parameter)
        {
            try
            {
                string nextStep = _gtfTaxRefundService.Current.SelectedRefundWayCode;

                await OnStepNext?.Invoke(nextStep);
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
        #endregion
    }
}
