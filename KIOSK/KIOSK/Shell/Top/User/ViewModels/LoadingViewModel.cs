using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.Media;
using KIOSK.Infrastructure.Logging;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KIOSK.ViewModels;

namespace KIOSK.Shell.Top.Main.ViewModels;

public partial class LoadingViewModel : ObservableObject, INavigable
{
    private readonly ILoggingService _logging;
    private readonly IVideoPlayService _videoPlay;

    private Uri videoPath;
    
    [ObservableProperty]
    private Brush? backgroundBrush;

    public LoadingViewModel(ILoggingService logging, IVideoPlayService videoPlay)
    {
        _logging = logging;
        _videoPlay = videoPlay;

        // TODO: 로딩 시 필요한 작업 수행
        try
        {
            // TODO: 파일 존재 유무 체크
            videoPath = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "Loading.mp4"), UriKind.Absolute);
        }
        catch (IOException ex)
        {
            // 파일을 찾지 못했을 때
            _logging?.Error(ex, ex.Message);
        }
        catch (Exception ex)
        {
            // 그 외 예외
            _logging?.Error(ex, ex.Message);
        }

        BackgroundBrush = _videoPlay.BackgroundBrush;
        _videoPlay.SetSource(videoPath, loop: true, mute: true, autoPlay: true);
    }

    public async Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        // TODO: 로딩 시 필요한 작업 수행
        try
        {
            // TODO: 파일 존재 유무 체크
            videoPath = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "Loading.mp4"), UriKind.Absolute);
        }
        catch (IOException ex)
        {
            // 파일을 찾지 못했을 때
            _logging?.Error(ex, ex.Message);
        }
        catch (Exception ex)
        {
            // 그 외 예외
            _logging?.Error(ex, ex.Message);
        }

        BackgroundBrush = _videoPlay.BackgroundBrush;
        _videoPlay.SetSource(videoPath, loop: true, mute: true, autoPlay: true);

        // 초기화 완료될 때까지 대기 (AppBootstrapper에서 실행됨)
        //await _initState.Initialization;
    }

    public async Task OnUnloadAsync()
    {
        // TODO: 언로드 시 필요한 작업 수행
        _videoPlay.Stop();
    }

}
