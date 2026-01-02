using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.UI.Navigation;
using KIOSK.Services.DataBase;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using KIOSK.ViewModels;

namespace KIOSK.Modules.Features.Environment.ViewModel
{
    public partial class CassetteModel : ObservableObject
    {
        [ObservableProperty] private int id;
        [ObservableProperty] private string deviceId;
        [ObservableProperty] private int slot;
        [ObservableProperty] private string currency;
        [ObservableProperty] private int value;
        [ObservableProperty] private int count;
    }

    public partial class EnvironmentCassetteSettingViewModel : ObservableObject, INavigable
    {
        // DataGrid에 바인딩되는 카세트 리스트
        public ObservableCollection<WithdrawalCassetteModel> WithdrawalCassette { get; set; }

        // ComboBox에 바인딩되는 통화 리스트
        public ObservableCollection<string> Currencies { get; set; } = new ObservableCollection<string> { "KRW", "USD", "JPY", "CNY" };

        [ObservableProperty] public string statusMessage;

        private int _nextId = 100;

        private readonly IPopupService _popup;
        private readonly WithdrawalCassetteServiceV2 _withdrawalCassetteService;

        public EnvironmentCassetteSettingViewModel(IPopupService popup, WithdrawalCassetteServiceV2 withdrawalCassetteService)
        {
            _popup = popup;
            _withdrawalCassetteService = withdrawalCassetteService;
            WithdrawalCassette = new ObservableCollection<WithdrawalCassetteModel>(_withdrawalCassetteService.GetSlotsAsync().Result);
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
        }

        public async Task OnUnloadAsync()
        {

        }


        // --- ICommand Methods ---
        // 기기별 슬롯 추가 로직
        [RelayCommand]
        private void AddSlotToDevice(object deviceIdObject)
        {
            //if (!(deviceIdObject is string deviceId)) return;

            //// 해당 기기의 현재 최대 슬롯 번호를 찾습니다.
            //var currentSlots = AllCassettes
            //    .Where(c => c.DeviceId == deviceId)
            //    .Select(c => c.Slot)
            //    .DefaultIfEmpty(0);

            //int newSlotNumber = currentSlots.Max() + 1;

            //if (newSlotNumber > 4)
            //{
            //    StatusMessage = $"{deviceId}에 더 이상 슬롯을 추가할 수 없습니다 (최대 4개).";
            //    return;
            //}

            //// 새 모델 인스턴스를 생성하고 ObservableCollection에 추가 (UI 자동 업데이트)
            //AllCassettes.Add(new CassetteModel
            //{
            //    Id = _nextId++,
            //    DeviceId = deviceId,
            //    Slot = newSlotNumber,
            //    Currency = string.Empty,
            //    Value = 0,
            //    Count = 0
            //});

            //StatusMessage = $"{deviceId}에 슬롯 {newSlotNumber}가 추가되었습니다. 저장하세요.";
        }

        // 슬롯 삭제 로직
        [RelayCommand]
        private void DeleteSlot(object item)
        {
            Trace.WriteLine(Currencies);

            if (item is WithdrawalCassetteModel cassette &&
                MessageBox.Show($"{cassette.DeviceID}의 슬롯 {cassette.Slot}을 삭제하시겠습니까?", "삭제 확인",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                WithdrawalCassette.Remove(cassette);
                StatusMessage = "슬롯이 삭제되었습니다. 저장하세요.";
            }


        }

        // 전체 저장 로직 (유효성 검사 및 데이터 처리)
        [RelayCommand]
        private async void SaveAll(object obj)
        {
            await _withdrawalCassetteService.SaveAsync(WithdrawalCassette);
        }

        [RelayCommand]
        private void Close(object obj)
        {
            _popup.CloseLocal();
        }

        // 데이터 유효성 검사 (예시)
        //private bool ValidateData()
        //{
        //    //// 예시: 슬롯 번호 중복 및 값 누락 검사
        //    //var validationErrors = AllCassettes
        //    //    .GroupBy(c => new { c.DeviceId, c.Slot })
        //    //    .Where(g => g.Count() > 1 || string.IsNullOrWhiteSpace(g.Key.DeviceId) ||
        //    //                g.All(c => c.Value <= 0 || string.IsNullOrWhiteSpace(c.Currency)));

        //    //if (validationErrors.Any())
        //    //{
        //    //    StatusMessage = "오류: 중복된 슬롯 번호나 누락된 통화/권종 값이 있습니다. 확인해주세요.";
        //    //    return false;
        //    //}

        //    //return true;
        //}
    }
}
