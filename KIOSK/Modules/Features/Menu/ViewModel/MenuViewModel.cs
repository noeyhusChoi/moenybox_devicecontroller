using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.Media;
using KIOSK.Infrastructure.Logging;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Devices.Management;
using KIOSK.FSM;
using KIOSK.Models;
using KIOSK.Services;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Infrastructure.UI.Navigation.State;
using KIOSK.Infrastructure.Database;
using KIOSK.Status;
using KIOSK.Shell.Sub.Exchange.ViewModel;
using KIOSK.Shell.Sub.Gtf.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;

namespace KIOSK.ViewModels
{
    public partial class MenuViewModel : ObservableObject, INavigable
    {
        private readonly IServiceProvider _provider;
        private readonly ILoggingService _logging;
        [ObservableProperty] private bool isActiveExchangeSell = false;

        public MenuViewModel(IServiceProvider provider, ILoggingService logging)
        {
            _provider = provider;
            _logging = logging;

            // 최초 한 번은 즉시 체크
            _ = RefreshStatusAsync();
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
        private async Task RefreshStatusAsync()
        {
            try
            {
                var statusStore = _provider.GetRequiredService<IStatusStore>();
                var snapshots = statusStore.GetAll();

                // 1) 장치 상태 체크 (IDSCANNER / DEPOSIT / HCDM)
                var deviceHasError = snapshots
                    .Where(x =>
                        x.Name.StartsWith("IDSCANNER") ||
                        x.Name.StartsWith("DEPOSIT") ||
                        x.Name.StartsWith("HCDM"))
                    .Any(x => (x.Alerts?.Count ?? 0) > 0 || x.Health == DeviceHealth.Offline);

                // 2) 네트워크 체크 (예시: CEMS API 핑)
                var networkOk = true;
                //try
                //{
                //    var apiPing = _provider.GetService<IApiHealthCheckService>();
                //    if (apiPing != null)
                //    {
                //        networkOk = await apiPing.PingAsync();
                //    }
                //}
                //catch (Exception ex)
                //{
                //    networkOk = false;
                //    _logging.Error(ex, "Network health check failed.");
                //}

                // 3) DB 체크 (예시: 단순 쿼리 한 번)
                var dbOk = true;
                try
                {
                    var dbHealth = _provider.GetService<IDatabaseService>();
                    if (dbHealth != null)
                    {
                        dbOk = await dbHealth.CanConnectAsync();
                    }
                }
                catch (Exception ex)
                {
                    dbOk = false;
                    _logging.Error(ex, "DB health check failed.");
                }

                // 4) 최종 서비스 사용 가능 여부 결정
                IsActiveExchangeSell = !deviceHasError && networkOk && dbOk;

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
                _logging.Error(ex, "RefreshStatusAsync failed.");
                // 오류나면 보수적으로 버튼 잠그기
                IsActiveExchangeSell = false;
            }
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
                    case "1":
                        await nav.SwitchSubShell<ExchangeShellViewModel>();
                        break;
                    case "2":
                        break;
                    case "3":
                        await nav.SwitchSubShell<GtfSubShellViewModel>();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
