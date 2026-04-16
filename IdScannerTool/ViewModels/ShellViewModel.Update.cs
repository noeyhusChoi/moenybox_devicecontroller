using System.Windows;

namespace IdScannerTool.ViewModels;

public partial class ShellViewModel
{
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
        _updateOverlay.IsBusy = true;
        _updateOverlay.ShowProgress = true;
        _updateOverlay.ProgressPercent = 0;
        _updateOverlay.StatusMessage = $"버전 {checkResult.Update.Version} 다운로드 중입니다. 0%";

        try
        {
            await _appUpdateService.DownloadAndApplyAsync(
                checkResult.Update,
                progress =>
                {
                    var dispatcher = Application.Current?.Dispatcher;

                    void UpdateProgress()
                    {
                        _updateOverlay.ProgressPercent = progress;
                        _updateOverlay.StatusMessage = $"버전 {checkResult.Update.Version} 다운로드 중입니다. {progress}%";
                    }

                    if (dispatcher is null || dispatcher.CheckAccess())
                    {
                        UpdateProgress();
                        return;
                    }

                    dispatcher.Invoke(UpdateProgress);
                });
        }
        catch (Exception ex)
        {
            _updateOverlay.IsBusy = false;
            _updateOverlay.ShowProgress = false;
            _updateOverlay.StatusMessage = ex.Message;
        }
        finally
        {
            _updateBusy = false;
        }
    }
}
