using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Infrastructure.Management.Devices;

namespace KIOSK.ViewModels
{
    public partial class ExchangeIDScanCompleteViewModel : ObservableObject, IStepMain, IStepNext, IStepError, INavigable
    {
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        private readonly IDeviceManager _deviceManager;

        [ObservableProperty]
        private bool canNext = false; // true면 활성, false면 비활성

        public ExchangeIDScanCompleteViewModel(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            var scanTask = Task.Run(async () =>
            {
                try
                {
                    int maintainCount = 0;

                    while (true)
                    {
                        var res = await _deviceManager.SendAsync("IDSCANNER1", new DeviceCommand("ScanStart"));

                        if (res != null && res.Success == true)
                        {
                            res = await _deviceManager.SendAsync("IDSCANNER1", new DeviceCommand("GetScanStatus"));

                            if (res.Data is Pr22.Util.PresenceState state)
                            {
                                switch (state)
                                {
                                    case Pr22.Util.PresenceState.Empty:
                                        if (maintainCount > 3)
                                            return true;

                                        maintainCount++;
                                        break;

                                    case Pr22.Util.PresenceState.Dirty:
                                    case Pr22.Util.PresenceState.Moving:
                                    case Pr22.Util.PresenceState.Present:
                                    case Pr22.Util.PresenceState.NoMove:
                                        maintainCount = 0;
                                        break;
                                }
                            }
                        }

                        await Task.Delay(200);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            });

            // 10초 제한 두기
#if DEBUG
            var completed = await Task.WhenAny(scanTask, Task.Delay(TimeSpan.FromSeconds(2)));
#else
            var completed = await Task.WhenAny(scanTask, Task.Delay(TimeSpan.FromSeconds(10)));
#endif
            CanNext = true;
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }


        #region Commands
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
        private async Task Next()
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
