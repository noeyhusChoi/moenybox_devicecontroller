using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfAlipayAccountSelectViewModel : StepViewModelBase
    {
        private readonly IDeviceManager _deviceManager;
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfTaxRefundModel Current => _gtfTaxRefundService.Current;

        [ObservableProperty]
        public string inputNumber = "";

        public GtfAlipayAccountSelectViewModel(IDeviceManager deviceManager, GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _deviceManager = deviceManager;
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            // TODO: 테스트 데이터 삭제 필요
            Current.AlipayUsers.Clear();
            Current.AlipayUsers.Add(new AlipayUser
            {
                UserId = "1234",
                UserName = "asdf",
                LoginId = "qwer"
            });
            Current.AlipayUsers.Add(new AlipayUser
            {
                UserId = "1234",
                UserName = "asdf",
                LoginId = "qwer"
            });
            Current.AlipayUsers.Add(new AlipayUser
            {
                UserId = "1234",
                UserName = "asdf",
                LoginId = "qwer"
            });

            return Task.CompletedTask;
        }

        public override Task OnUnloadAsync() => Task.CompletedTask;

        #region Commands
        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private async Task Next(object? o)
        {
            try
            {
                if (o is string param)
                {
                    Trace.WriteLine(param);
                }

                await ExecuteStepAsync(OnStepNext, o);
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }
        #endregion
    }
}
