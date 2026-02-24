using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.Media;
using KIOSK.Application.Services.Devices;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Domain.Entities;
using KIOSK.Application.Services;
using KIOSK.Presentation.Navigation.Services;
using Microsoft.Extensions.Logging;
using KIOSK.Presentation.Features.Exchange.Layout.ViewModels;
using KIOSK.Presentation.Features.GTF.Layout.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;
using KIOSK.Presentation.Features.ExchangeV2.Layout.ViewModels;
using KIOSK.Presentation.Features.MenuV2.Layout.ViewModels;
using KIOSK.Presentation.Abstractions;
using KIOSK.Infrastructure.Health;

namespace KIOSK.Presentation.Features.MenuV2.Pages.ViewModels
{
    public partial class MenuV2ViewModel : ObservableObject, IViewLifecycle
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<MenuV2ViewModel> _logger;
        [ObservableProperty] private bool isActiveExchangeSell = true;

        public MenuV2ViewModel(IServiceProvider provider, ILogger<MenuV2ViewModel> logger)
        {
            _provider = provider;
            _logger = logger;

            // 최초 한 번은 즉시 체크
            //_ = RefreshStatusAsync();
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            
            // DB : 거래 내역, 환율 정보
            // 네트워크 : 외부 API 연동
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }

        /// <summary>
        /// 주기적으로 호출해서 장치/네트워크/DB 상태를 갱신
        /// </summary>
        private Task RefreshStatusAsync()
        {
            try
            {
                var statusService = _provider.GetRequiredService<IDeviceStatusPort>();
                var snapshots = statusService.GetAllSnapshots();

                // 1) 장치 상태 체크 (IDSCANNER / DEPOSIT / HCDM)
                var deviceHasError = snapshots
                    .Where(x =>
                        x.Name.StartsWith("IDSCANNER") ||
                        x.Name.StartsWith("DEPOSIT") ||
                        x.Name.StartsWith("WITHDRAWAL"))
                    .Any(x => (x.Alerts?.Count ?? 0) > 0 || x.Health == DeviceHealth.Offline);

                // 2) 네트워크 체크 (Status 파이프라인 기반)
                var networkSnapshot = snapshots.FirstOrDefault(x =>
                    string.Equals(x.Name, SystemHealthSourceIds.Network, StringComparison.OrdinalIgnoreCase));
                var networkOk = networkSnapshot is not null
                                && networkSnapshot.Health != DeviceHealth.Offline
                                && (networkSnapshot.Alerts?.Count ?? 0) == 0;

                // 3) 디스크 체크 (Status 파이프라인 기반)
                var diskSnapshot = snapshots.FirstOrDefault(x =>
                    string.Equals(x.Name, SystemHealthSourceIds.Disk, StringComparison.OrdinalIgnoreCase));
                var diskOk = diskSnapshot is not null
                             && diskSnapshot.Health != DeviceHealth.Offline
                             && (diskSnapshot.Alerts?.Count ?? 0) == 0;

                // 4) 최종 서비스 사용 가능 여부 결정 (DB 체크 제외)
                IsActiveExchangeSell = !deviceHasError && networkOk && diskOk;

                // 디버깅용 로그
                foreach (var snap in snapshots)
                {
                    Trace.WriteLine(
                        $"[{snap.Name}] {snap.Health} " +
                        $"{string.Join(", ", snap.Alerts.Select(a => $"{a.Code}:{a.Message}"))}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefreshStatusAsync failed.");
                // 오류나면 보수적으로 버튼 잠그기
                IsActiveExchangeSell = false;
            }

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task Next(object? parameter)
        {
            var billPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sound", "Click.wav");
            var audio = _provider.GetRequiredService<IAudioPlayService>();
            audio.Play(billPath);

            var nav = _provider.GetRequiredService<INavigationService>();
            // TODO: 여기도 정형화, 하드코딩 제거
            if (parameter is string param)
            {
                switch (param)
                {
                    case "CARD":
                        await nav.NavigateLayout<ExchangeLayoutViewModel>();
                        break;
                    case "EXCHANGE":
                        await nav.NavigateLayout<ExchangeV2LayoutViewModel>();
                        break;
                    case "TAXFREE":
                        await nav.NavigateLayout<GtfLayoutViewModel>();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
