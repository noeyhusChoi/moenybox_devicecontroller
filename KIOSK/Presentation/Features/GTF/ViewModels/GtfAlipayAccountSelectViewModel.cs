using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Domain.Entities;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.GTF.ViewModels
{
    public partial class GtfAlipayAccountSelectViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        private readonly IDeviceManager _deviceManager;
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfTaxRefundModel Current => _gtfTaxRefundService.Current;

        [ObservableProperty]
        public string inputNumber = "";

        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        public GtfAlipayAccountSelectViewModel(IDeviceManager deviceManager, GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _deviceManager = deviceManager;
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
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
        }

        public async Task OnUnloadAsync()
        {
        
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
        private async Task Next(object? o)
        {
            try
            {
                if(o is string param)
                Trace.WriteLine(param);
                if (OnStepNext is not null)
                    await OnStepNext("");
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
