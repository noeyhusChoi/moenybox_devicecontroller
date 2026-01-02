using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.UI;
using KIOSK.Services;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace KIOSK.Shell.Top.Main.ViewModels;

public partial class FooterViewModel : ObservableObject
{
    [ObservableProperty]
    private ImageSource locationQr;

    [ObservableProperty]
    private string branchName = "서울역점";

    [ObservableProperty]
    private string date = DateTime.Now.ToString("yyyy.MM.dd. (ddd)").ToUpper();

    [ObservableProperty]
    private string time;

    [ObservableProperty]
    private Uri locationIconUri;

    private readonly System.Timers.Timer timer; // System.Timers.Timer
    private bool disposed;

    private readonly IInactivityService _inactivityService;
    public IInactivityService Timer => _inactivityService;

    public FooterViewModel(IServiceProvider serviceProvider)
    {
        var QrService = serviceProvider.GetRequiredService<IQrGenerateService>();
        LocationQr = QrService.ImageFromBytes();

        _inactivityService = serviceProvider.GetRequiredService<IInactivityService>();

        Time = DateTime.Now.ToString("HH:mm:ss");

        LocationIconUri = new Uri("pack://application:,,,/Assets/Image/Vector.png", UriKind.Absolute);
        timer = new System.Timers.Timer(500); // 500ms 간격으로 체크 (UI 업데이트는 초단위로만)
        timer.AutoReset = true;
        timer.Elapsed += Timer_Elapsed;
        timer.Start();
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        // DateTime.Now는 타이머 스레드에서 안전하게 읽을 수 있음
        var now = DateTime.Now;
        var newTime = now.ToString("HH:mm:ss");

        // 불필요한 UI 호출을 줄이기 위해 값이 바뀌었을 때만 디스패치
        if (newTime != Time)
        {
            // UI 스레드로 안전하게 값 갱신
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                Time = newTime;
            }, DispatcherPriority.Render);
        }

        // 날짜 변경 처리(자정 등) 필요하면 비슷한 방식으로 처리
        var newDate = now.ToString("yyyy.MM.dd. (ddd)").ToUpper();
        if (newDate != Date)
        {
            Application.Current?.Dispatcher?.InvokeAsync(() => Date = newDate, DispatcherPriority.Background);
        }
    }
}
