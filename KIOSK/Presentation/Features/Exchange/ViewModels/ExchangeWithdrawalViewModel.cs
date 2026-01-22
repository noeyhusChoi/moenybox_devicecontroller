using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Exchange;
using KIOSK.Presentation.Features.Exchange.Resources;
using KIOSK.Presentation.Shared.Abstractions;
namespace KIOSK.ViewModels
{
    public partial class ExchangeWithdrawalViewModel : ObservableObject, IStepNext, INavigable
    {
        //public Func<Task>? OnStepMain { get; set; }
        //public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

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

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            try
            {
                await _withdrawalUseCase.ExecuteAsync(ct);
                await Next(true);
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }

        #region Commands
        [RelayCommand]
        private async Task Next(object? o)
        {
            try
            {
                OnStepNext?.Invoke("");
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
        #endregion
    }
}
