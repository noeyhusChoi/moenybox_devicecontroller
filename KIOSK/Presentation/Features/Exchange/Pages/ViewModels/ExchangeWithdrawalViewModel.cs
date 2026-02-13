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
    public partial class ExchangeWithdrawalViewModel : PageViewModelBase
    {

        [ObservableProperty]
        private string videoPath;

        private readonly IExchangeWithdrawalUseCase _withdrawalUseCase;
        private readonly IExchangeLoadingVideoProvider _loadingVideoProvider;

        public ExchangeWithdrawalViewModel(
            IExchangeWithdrawalUseCase withdrawalUseCase,
            IExchangeLoadingVideoProvider loadingVideoProvider)
        {
            _withdrawalUseCase = withdrawalUseCase;
            _loadingVideoProvider = loadingVideoProvider;

            // TODO: 로딩 시 필요한 작업 수행
            VideoPath = _loadingVideoProvider.GetLoadingVideoPath();
        }

        public override async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            try
            {
                await _withdrawalUseCase.ExecuteAsync(ct);
                await Next(true);
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }

        public override Task OnUnloadAsync() => Task.CompletedTask;

        #region Commands
        [RelayCommand]
        private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);
        #endregion
    }
}
