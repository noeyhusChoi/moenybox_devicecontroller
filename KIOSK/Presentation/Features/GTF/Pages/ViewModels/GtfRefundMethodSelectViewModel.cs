using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfRefundMethodSelectViewModel : StepViewModelBase
    {

        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfRefundMethodSelectViewModel(IGtfTaxRefundService gtfTaxRefundService)
        {
            _gtfTaxRefundService = gtfTaxRefundService;
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct) => Task.CompletedTask;

        public override Task OnUnloadAsync() => Task.CompletedTask;

        #region Commands
        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);


        [RelayCommand]
        private async Task Next(object? parameter)
        {
            if (parameter is not string refundMethod)
                return;

            try
            {
                _gtfTaxRefundService.Current.SelectedRefundWayCode = refundMethod;

                Trace.WriteLine($"Selected refund method: {_gtfTaxRefundService.Current.SelectedRefundWayCode}");

                await ExecuteStepAsync(OnStepNext, refundMethod);
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
        #endregion
    }
}
