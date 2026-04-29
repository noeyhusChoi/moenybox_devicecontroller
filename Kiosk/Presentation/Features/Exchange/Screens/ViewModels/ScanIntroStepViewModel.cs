using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace Kiosk.ViewModels.Steps;

public sealed partial class ScanIntroStepViewModel : ExchangeStepViewModelBase, IScanIntroStepViewModel
{
    private static readonly string DefaultPreviewVideoPath = CreateAssetPath("Video", "IDScan_ID.mp4");

    public ScanIntroStepViewModel()
    {
        PreviewVideoPath = DefaultPreviewVideoPath;
    }

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
