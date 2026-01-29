using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Shared.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels
{
    public partial class ExchangeV2ExchangeMethodSelectViewModel : StepViewModelBase
    {
        public ExchangeV2ExchangeMethodSelectViewModel()
        {
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            // TODO: 로딩 시 필요한 작업 수행
            return Task.CompletedTask;
        }

        public override Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
            return Task.CompletedTask;
        }

        #region Commands

        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private Task Next(object? parameter)
            => ExecuteStepAsync(OnStepNext, parameter);

        #endregion
    }
}
