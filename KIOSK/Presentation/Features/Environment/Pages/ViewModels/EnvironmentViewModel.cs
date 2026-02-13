using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Storage;
using System.Collections.ObjectModel;
using KIOSK.Presentation.Features.Environment.Pages.ViewModels;
using KIOSK.Presentation.Navigation.Services;

namespace KIOSK.Presentation.Features.Environment.Pages.ViewModels
{
    public partial class EnvironmentViewModel : ObservableObject
    {
        private readonly IDeviceCommandService _deviceCommandService;
        private readonly IDeviceStatusService _deviceStatusService;
        private readonly IPopupService _popup;
        private readonly INavigationService _nav;

        public ObservableCollection<StatusSnapshot> DeviceStatuses { get; } = new();
        public ObservableCollection<WithdrawalCassette> WithdrawalCassettes { get; } = new();
        public ObservableCollection<StorageInfo> StorageList { get; } = new();

        private readonly WithdrawalCassetteService _withdrawalCassetteService;

        public EnvironmentViewModel(
            IDeviceCommandService deviceCommandService,
            IDeviceStatusService deviceStatusService,
            WithdrawalCassetteService withdrawalCassetteService,
            IStorageService storageService,
            IPopupService popup,
            INavigationService nav)
        {
            _deviceCommandService = deviceCommandService;
            _deviceStatusService = deviceStatusService;
            _withdrawalCassetteService = withdrawalCassetteService; // 시재
            _popup = popup;
            _nav = nav;

            // 초기 스냅샷 로드
            DeviceStatuses = new ObservableCollection<StatusSnapshot>(_deviceStatusService.GetAllSnapshots());

            // 스냅샷 구독
            _deviceStatusService.StatusUpdated += OnStatusUpdated;

            // 시재 정보 로드
            _ = RefreshCassetteInfoAsync();

            // 시스템 용량
            StorageList = new ObservableCollection<StorageInfo>(storageService.GetAllDrives());
        }

        private void OnStatusUpdated(string name, StatusSnapshot snapshot)
        {
            // Dispatcher로 UI 스레드 보장
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var existing = DeviceStatuses.FirstOrDefault(s => s.Name == name);
                if (existing is not null)
                {
                    var idx = DeviceStatuses.IndexOf(existing);
                    DeviceStatuses[idx] = snapshot;   // 레코드 통째로 교체
                }
                else
                {
                    DeviceStatuses.Add(snapshot);
                }
            });
        }

        private async Task RefreshCassetteInfoAsync()
        {
            await _withdrawalCassetteService.InitializeAsync();

            // Dispatcher로 UI 스레드 보장
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                WithdrawalCassettes.Clear(); // 기존 데이터 클리어(선택사항)

                foreach (var snap in _withdrawalCassetteService.Get())
                    WithdrawalCassettes.Add(snap);
            });
        }

        [RelayCommand]
        private async Task ResetDeviceCommand(object name)
        {
            if (name is string device)
            {
                // TODO: 공통 명령어 정리 (INIT, START, STOP ...) 아마도 INIT만 필요할 듯
                await _deviceCommandService.SendAsync(device, new DeviceCommand("Init"));
            }
        }

        [RelayCommand]
        private Task Withdrawal()
        {
            //var receiptService = _provider.GetRequiredService<ReceiptPrintService>();
            //await receiptService.PrintReceiptAsync("en-US", new TransactionModelV2());

            //var api = _provider.GetRequiredService<CemsApiService>();
            //var cassette = _provider.GetRequiredService<WithdrawalCassetteService>();

            //await cassette.InitializeAsync();
            //var result = await api.SetCashAsync(cassette.Get(), default);

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task WithdrawalCassette()
        {
            _popup.ShowPopup<EnvironmentCassetteSettingViewModel>();
        }

        [RelayCommand]
        private async Task OpenDeviceStatus()
        {
            await _nav.NavigatePage<DeviceStatusViewModel>();
        }

        [RelayCommand]
        private async Task OpenResxLocalizationTest()
        {
            await _nav.NavigatePage<ResxLocalizationTestViewModel>();
        }
    }
}
