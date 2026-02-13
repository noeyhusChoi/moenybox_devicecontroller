using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels
{
    public partial class ExchangeIDScanCompleteViewModel : PageViewModelBase
    {
        private readonly IDeviceCommandService _deviceCommandService;

        [ObservableProperty]
        private bool canNext = false; // true면 활성, false면 비활성

        public ExchangeIDScanCompleteViewModel(IDeviceCommandService deviceCommandService)
        {
            _deviceCommandService = deviceCommandService;
        }

        public override async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            var scanTask = Task.Run(async () =>
            {
                try
                {
                    int maintainCount = 0;

                    while (true)
                    {
                        var res = await _deviceCommandService.SendAsync("IDSCANNER1", new DeviceCommand("ScanStart"));

                        if (res != null && res.Success == true)
                        {
                            res = await _deviceCommandService.SendAsync("IDSCANNER1", new DeviceCommand("GetScanStatus"));

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

        public override Task OnUnloadAsync() => Task.CompletedTask;


        #region Commands
        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);
        #endregion
    }
}
