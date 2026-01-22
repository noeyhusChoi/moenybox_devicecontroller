using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using KIOSK.Infrastructure.Common.Utils;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public sealed class ExchangeDepositAssetsProvider : IExchangeDepositAssetsProvider
    {
        private readonly string _assetsDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets",
            "Image",
            "Denomination");

        private const int MaxCount = 7;
        private static readonly string[] SupportedExtensions = { ".png", ".jpg" };

        public Task<ExchangeDepositAssets> LoadAsync(string currencyCode, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var notes = new List<ExchangeDepositNoteAsset>();

            if (!string.IsNullOrWhiteSpace(currencyCode) && Directory.Exists(_assetsDir))
            {
                var files = Directory.GetFiles(_assetsDir)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .Where(f => Path.GetFileName(f).StartsWith(currencyCode + "_", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                var list = files.Select(f =>
                    {
                        var name = Path.GetFileNameWithoutExtension(f);
                        var m = Regex.Match(name, @"^.+[_\-](\d+)$");
                        var denom = 0;
                        if (m.Success)
                            int.TryParse(m.Groups[1].Value, out denom);
                        return new { File = f, Denom = denom };
                    })
                    .OrderBy(x => x.Denom == 0 ? int.MaxValue : x.Denom)
                    .ThenBy(x => x.File)
                    .Take(MaxCount)
                    .ToArray();

                foreach (var item in list)
                {
                    ct.ThrowIfCancellationRequested();

                    var bmp = ImageCacheExtension.GetOrAdd(item.File, () =>
                    {
                        try
                        {
                            using var fs = new FileStream(item.File, FileMode.Open, FileAccess.Read, FileShare.Read);
                            var bi = new BitmapImage();
                            bi.BeginInit();
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                            bi.DecodePixelWidth = 240;
                            bi.StreamSource = fs;
                            bi.EndInit();
                            bi.Freeze();
                            return bi;
                        }
                        catch
                        {
                            return null;
                        }
                    });

                    notes.Add(new ExchangeDepositNoteAsset
                    {
                        Denomination = item.Denom,
                        Image = bmp,
                        FilePath = item.File
                    });
                }
            }

            var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Webp", "Guide_Deposit_AUD.webp");
            return Task.FromResult(new ExchangeDepositAssets(videoPath, notes));
        }
    }
}
