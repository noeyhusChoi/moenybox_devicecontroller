using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Exchange;
using KIOSK.Presentation.Features.Exchange.Resources;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels
{
    public partial class ExchangeIDScanProcessViewModel : PageViewModelBase
    {

        [ObservableProperty]
        private string videoPath;

        private readonly IExchangeIdScanUseCase _idScanUseCase;
        private readonly IExchangeLoadingVideoProvider _loadingVideoProvider;

        public ExchangeIDScanProcessViewModel(
            IExchangeIdScanUseCase idScanUseCase,
            IExchangeLoadingVideoProvider loadingVideoProvider)
        {
            _idScanUseCase = idScanUseCase;
            _loadingVideoProvider = loadingVideoProvider;

            VideoPath = _loadingVideoProvider.GetLoadingVideoPath();
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            _ = Task.Run(() => InitAsync(ct), ct);
            return Task.CompletedTask;
        }

        public override Task OnUnloadAsync() => Task.CompletedTask;

        private async Task InitAsync(CancellationToken ct)
        {
            try
            {
                var success = await _idScanUseCase.ScanAsync(ct);
                if (success)
                    await ExecuteStepAsync(OnStepNext, true);
                else
                    await ExecuteStepAsync(OnStepPrevious);
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }

        [RelayCommand]
        private async Task Loaded(object parameter)
        {
            try
            {
                var success = await _idScanUseCase.ScanAsync(CancellationToken.None);
                if (success)
                    await ExecuteStepAsync(OnStepNext, true);
                else
                    await ExecuteStepAsync(OnStepPrevious);
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }

        #region Commands
        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);
        #endregion
    }
}
