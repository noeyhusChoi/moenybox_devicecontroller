using System;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public sealed class ExchangeIdScanInfoAssets
    {
        public ExchangeIdScanInfoAssets(Uri imageUri, string videoPath)
        {
            ImageUri = imageUri;
            VideoPath = videoPath;
        }

        public Uri ImageUri { get; }
        public string VideoPath { get; }
    }
}
