using System.Windows;
using IdScannerTool.Services;

namespace IdScannerTool.ViewModels;

public partial class ShellViewModel
{
    private CancellationTokenSource? _updateDownloadCts;
    private PendingAppUpdate? _downloadedUpdate;

    private async Task StartUpdateAsync()
    {
        if (_updateBusy || _updateOverlay is null)
        {
            return;
        }

        var checkResult = await _appUpdateService.CheckForUpdatesAsync();
        ApplyUpdateCheckResult(checkResult);
        if (!checkResult.IsConfigured || !checkResult.IsUpdateAvailable || checkResult.Update is null)
        {
            return;
        }

        _updateBusy = true;
        _downloadedUpdate = null;
        _updateDownloadCts?.Dispose();
        _updateDownloadCts = new CancellationTokenSource();

        _updateOverlay.IsBusy = true;
        _updateOverlay.CanClose = false;
        _updateOverlay.CanUpdate = false;
        _updateOverlay.CanCancel = true;
        _updateOverlay.CanRestart = false;
        _updateOverlay.ShowProgress = true;
        _updateOverlay.ProgressPercent = 0;
        _updateOverlay.CloseButtonText = "닫기";
        _updateOverlay.UpdateButtonText = "업데이트";
        _updateOverlay.StatusMessage = $"버전 {checkResult.Update.Version} 다운로드 중입니다.";

        try
        {
            await _appUpdateService.DownloadUpdateAsync(
                checkResult.Update,
                progress =>
                {
                    var dispatcher = Application.Current?.Dispatcher;

                    void UpdateProgress()
                    {
                        _updateOverlay.ProgressPercent = progress;
                        _updateOverlay.StatusMessage = $"버전 {checkResult.Update.Version} 다운로드 중입니다.";
                    }

                    if (dispatcher is null || dispatcher.CheckAccess())
                    {
                        UpdateProgress();
                        return;
                    }

                    dispatcher.Invoke(UpdateProgress);
                },
                _updateDownloadCts.Token);

            _downloadedUpdate = checkResult.Update;
            _updateOverlay.IsBusy = false;
            _updateOverlay.CanClose = false;
            _updateOverlay.CanCancel = false;
            _updateOverlay.CanRestart = true;
            _updateOverlay.StatusMessage = "다운로드가 완료되었습니다. 지금 재시작해서 업데이트를 적용하세요.";
        }
        catch (OperationCanceledException)
        {
            _updateOverlay.IsBusy = false;
            _updateOverlay.CanClose = true;
            _updateOverlay.CanCancel = false;
            _updateOverlay.CanRestart = false;
            _updateOverlay.CanUpdate = true;
            _updateOverlay.UpdateButtonText = "다시 시도";
            _updateOverlay.ShowProgress = false;
            _updateOverlay.StatusMessage = "다운로드가 취소되었습니다.";
        }
        catch (Exception ex)
        {
            _updateOverlay.IsBusy = false;
            _updateOverlay.CanClose = true;
            _updateOverlay.CanCancel = false;
            _updateOverlay.CanRestart = false;
            _updateOverlay.CanUpdate = true;
            _updateOverlay.UpdateButtonText = "다시 시도";
            _updateOverlay.ShowProgress = false;
            _updateOverlay.StatusMessage = ex.Message;
        }
        finally
        {
            _updateBusy = false;
            _updateDownloadCts?.Dispose();
            _updateDownloadCts = null;
        }
    }

    private void CancelUpdateDownload()
    {
        _updateDownloadCts?.Cancel();
    }

    private void RestartAfterUpdateDownload()
    {
        if (_downloadedUpdate is null)
        {
            return;
        }

        _appUpdateService.ApplyUpdateAndRestart(_downloadedUpdate);
    }
}
