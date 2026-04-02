using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IdScannerTool.ViewModels;

public partial class SerialRegistrationViewModel : ObservableObject
{
    private readonly Func<Task<(bool Success, string? Serial, string Message)>> _extractFunc;
    private readonly Func<string, Task<(bool Success, string Message)>> _registerFunc;
    private readonly Func<Task> _retryFunc;

    public SerialRegistrationViewModel(
        Func<Task<(bool Success, string? Serial, string Message)>> extractFunc,
        Func<string, Task<(bool Success, string Message)>> registerFunc,
        Func<Task> retryFunc)
    {
        _extractFunc = extractFunc;
        _registerFunc = registerFunc;
        _retryFunc = retryFunc;
    }

    [ObservableProperty]
    private string registrationStatusMessage = "장치를 활성화하고 API 키를 저장하세요.";

    [ObservableProperty]
    private string registeredSerialKey = "-";

    [ObservableProperty]
    private string extractedSerialKey = "-";

    [ObservableProperty]
    private bool canRegisterSerial;

    [ObservableProperty]
    private bool isBusy;

    partial void OnCanRegisterSerialChanged(bool value)
        => RegisterSerialKeyCommand.NotifyCanExecuteChanged();

    partial void OnIsBusyChanged(bool value)
        => RegisterSerialKeyCommand.NotifyCanExecuteChanged();

    private bool CanRegisterNow()
        => CanRegisterSerial && !IsBusy;

    public void SetState(string? registered, string? extracted, string message, bool canRegister)
    {
        RegisteredSerialKey = string.IsNullOrWhiteSpace(registered) ? "-" : registered;
        ExtractedSerialKey = string.IsNullOrWhiteSpace(extracted) ? "-" : extracted;
        RegistrationStatusMessage = message;
        CanRegisterSerial = canRegister;
    }

    [RelayCommand]
    private Task ExtractAndPrepareAsync()
        => RunSafeAsync(async () =>
        {
            RegistrationStatusMessage = "장치 연결 + 시리얼 추출 진행 중...";
            var result = await _extractFunc();
            if (!result.Success)
            {
                RegistrationStatusMessage = result.Message;
                CanRegisterSerial = false;
                return;
            }

            ExtractedSerialKey = string.IsNullOrWhiteSpace(result.Serial) ? "-" : result.Serial;
            RegistrationStatusMessage = result.Message;
            CanRegisterSerial = !string.IsNullOrWhiteSpace(result.Serial);
        });

    [RelayCommand(CanExecute = nameof(CanRegisterNow))]
    private Task RegisterSerialKeyAsync()
        => RunSafeAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(ExtractedSerialKey) || ExtractedSerialKey == "-")
            {
                RegistrationStatusMessage = "등록할 시리얼이 없습니다.";
                return;
            }

            var result = await _registerFunc(ExtractedSerialKey);
            RegistrationStatusMessage = result.Message;
            if (!result.Success)
            {
                return;
            }

            await _retryFunc();
        });

    [RelayCommand]
    private Task RetryStartupFlowAsync()
        => RunSafeAsync(_retryFunc);

    private async Task RunSafeAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            RegistrationStatusMessage = $"오류: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
