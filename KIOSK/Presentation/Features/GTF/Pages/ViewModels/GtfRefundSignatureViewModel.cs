using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.Domain.Entities;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfRefundSignatureViewModel : StepViewModelBase
    {
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfTaxRefundModel Current => _gtfTaxRefundService.Current;

        public GtfRefundSignatureViewModel(GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _gtfApiService = gtfApiService;
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
            try
            {
                string nextStep = _gtfTaxRefundService.Current.SelectedRefundWayCode;

                await ExecuteStepAsync(OnStepNext, nextStep);
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
        #endregion
    }
}
