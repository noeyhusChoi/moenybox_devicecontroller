using System;
using System.IO;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public sealed class ExchangeLoadingVideoProvider : IExchangeLoadingVideoProvider
    {
        private readonly string _videoPath;

        public ExchangeLoadingVideoProvider()
        {
            _videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "Loading.mp4");
        }

        public string GetLoadingVideoPath()
        {
            return File.Exists(_videoPath) ? _videoPath : string.Empty;
        }
    }
}
