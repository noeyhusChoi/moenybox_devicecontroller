using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Services;
using KIOSK.ViewModels;
using System.Diagnostics;

namespace KIOSK.Modules.GTF.ViewModels
{
    public partial class GtfRefundMethodSelectViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfRefundMethodSelectViewModel(IGtfTaxRefundService gtfTaxRefundService)
        {
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
                if (parameter is not string refundMethod)
                    return;
                _gtfTaxRefundService.Current.SelectedRefundWayCode = refundMethod;

                Trace.WriteLine($"Selected refund method: {_gtfTaxRefundService.Current.SelectedRefundWayCode}");

                await OnStepNext?.Invoke(refundMethod)!;
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
        #endregion
    }
}
