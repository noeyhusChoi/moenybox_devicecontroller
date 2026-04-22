using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace Kiosk.ViewModels.Steps;

public sealed partial class ScanIntroStepViewModel : ExchangeStepViewModelBase, IScanIntroStepViewModel
{
    private static readonly string DefaultPreviewVideoPath = CreateAssetPath("Video", "IDScan_ID.mp4");

    public ScanIntroStepViewModel()
    {
        Title = "신분증 스캔을 진행해주세요";
        IntroMessage = "신분증을 아래 안내 영상처럼 올려주세요";
        Body = "얼굴 사진이 있는 면을 아래로 하여\n스캐너에 올려주세요";
        PreviewVideoPath = DefaultPreviewVideoPath;
    }

    [ObservableProperty]
    private string introMessage = string.Empty;

    public bool CanProceed => true;

    public string PreviewVideoPath { get; }

    private static string CreateAssetPath(string folder, string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "Assets", folder, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Assets", folder, fileName);
    }
}
