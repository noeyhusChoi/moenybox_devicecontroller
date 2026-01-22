using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Exchange;
using KIOSK.Presentation.Features.Exchange.Resources;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.ViewModels
{
    public partial class ExchangeIDScanProcessViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

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

        public Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            _ = Task.Run(() => InitAsync(ct), ct);
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            return Task.CompletedTask;
        }

        private async Task InitAsync(CancellationToken ct)
        {
            try
            {
                var success = await _idScanUseCase.ScanAsync(ct);
                if (success)
                    await Next(true);
                else
                    await Previous();
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }

        [RelayCommand]
        private async Task Loaded(object parameter)
        {
            try
            {
                var success = await _idScanUseCase.ScanAsync(CancellationToken.None);
                if (success)
                    await Next(true);
                else
                    await Previous();
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
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
