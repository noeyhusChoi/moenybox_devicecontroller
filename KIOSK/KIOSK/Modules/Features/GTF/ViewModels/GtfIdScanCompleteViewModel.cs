using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.ViewModels;
using Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Modules.GTF.ViewModels
{
    public partial class GtfIdScanCompleteViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        private readonly ILocalizationService _localizationService;

        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }


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
        private async Task Next(object? o)
        {
            try
            {
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
