using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Devices.Management;
using KIOSK.Infrastructure.Storage;
using KIOSK.Services;
using System.Collections.ObjectModel;
using KIOSK.Infrastructure.UI.Navigation;
using KIOSK.Modules.Features.Environment.ViewModel;

namespace KIOSK.ViewModels
{
    public partial class EnvironmentViewModel : ObservableObject
    {
        private readonly IDeviceManager _deviceManagerV2;
        private readonly IPopupService _popup;
        
        public ObservableCollection<DeviceStatusSnapshot> DeviceStatuses { get; } = new();
        public ObservableCollection<WithdrawalCassette> WithdrawalCassettes { get; } = new();
        public ObservableCollection<StorageInfo> StorageList { get; } = new();

        private readonly WithdrawalCassetteService _withdrawalCassetteService;

        public EnvironmentViewModel(IDeviceManager deviceManagerV2, WithdrawalCassetteService withdrawalCassetteService, IStorageService storageService, IPopupService popup)
        {
            _deviceManagerV2 = deviceManagerV2; // 장비
            _withdrawalCassetteService = withdrawalCassetteService; // 시재
            _popup = popup;

            // 초기 스냅샷 로드
            DeviceStatuses = new ObservableCollection<DeviceStatusSnapshot>(_deviceManagerV2.GetLatestSnapshots());

            // 스냅샷 구독
            _deviceManagerV2.StatusUpdated += OnStatusUpdated;

            // 시재 정보 로드
            _ = RefreshCassetteInfoAsync();

            // 시스템 용량
            StorageList = new ObservableCollection<StorageInfo>(storageService.GetAllDrives());
        }

        private void OnStatusUpdated(string name, DeviceStatusSnapshot snapshot)
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
                await _deviceManagerV2.SendAsync(device, new DeviceCommand("Init"));
            }
        }

        [RelayCommand]
        private async void Withdrawal()
        {
            //var receiptService = _provider.GetRequiredService<ReceiptPrintService>();
            //await receiptService.PrintReceiptAsync("en-US", new TransactionModelV2());

            //var api = _provider.GetRequiredService<CemsApiService>();
            //var cassette = _provider.GetRequiredService<WithdrawalCassetteService>();

            //await cassette.InitializeAsync();
            //var result = await api.SetCashAsync(cassette.Get(), default);

        }

        [RelayCommand]
        private async Task WithdrawalCassette()
        {
            _popup.ShowLocal<EnvironmentCassetteSettingViewModel>();
        }
    }
}
