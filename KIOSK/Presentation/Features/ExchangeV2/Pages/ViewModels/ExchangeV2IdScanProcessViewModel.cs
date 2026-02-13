using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Features.ExchangeV2.Orchestration;
using KIOSK.Presentation.Abstractions;
using KIOSK.Presentation.Features.ExchangeV2.Popup.ViewModels;
using KIOSK.Presentation.Navigation.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels
{
    public partial class ExchangeV2IdScanProcessViewModel : PageViewModelBase
    {
        private const int MaxScanAttempts = 3;
        private readonly IExchangeV2Orchestrator _orchestrator;
        private readonly IPopupService _popup;
        private bool _scanStarted;
        private CancellationTokenSource? _scanCts;

        [ObservableProperty]
        private string videoPath;

        public ExchangeV2IdScanProcessViewModel(
            IExchangeV2Orchestrator orchestrator,
            IPopupService popup)
        {
            _orchestrator = orchestrator;
            _popup = popup;

            var videoFile = "IDScan_ID.mp4"; // TODO: 언어 정보로 영상 선택 (신분증/여권), 파일 존재 여부 확인
            VideoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", videoFile);
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            if (_scanStarted)
                return Task.CompletedTask;

            _scanStarted = true;
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = RunScanFlowAsync(_scanCts.Token);
            return Task.CompletedTask;
        }

        public override Task OnUnloadAsync()
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = null;
            _popup.ClosePopup();
            _scanStarted = false;
            return Task.CompletedTask;
        }

        private async Task RunScanFlowAsync(CancellationToken ct)
        {
            try
            {
                for (var attempt = 1; attempt <= MaxScanAttempts; attempt++)
                {
                    ct.ThrowIfCancellationRequested();

                    // 신분증 인식 -> OCR -> 한도 확인
                    var ok = await _orchestrator.ProcessIdentityAndLimitAsync(ct);
                    if (ok)
                    {
                        await ExecuteStepAsync(OnStepNext, null);
                        return;
                    }
                }

                ShowScanFailedPopup();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
                ShowScanFailedPopup();
            }
        }

        private void ShowScanFailedPopup()
        {
            _popup.ShowPopup<ExchangeV2IdScanFailedPopupViewModel>(vm =>
            {
                vm.Title = "신분증 인식에 실패했습니다.";
                vm.Message = "확인 후 동의 화면으로 이동해 다시 시도해 주세요.";
                vm.OnConfirmAsync = async () => await ExecuteStepAsync(OnStepPrevious, null);
            });
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
