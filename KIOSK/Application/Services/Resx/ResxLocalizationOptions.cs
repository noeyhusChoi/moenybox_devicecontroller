using System.Collections.Generic;
using System.Globalization;

namespace Localization.Resx
{
    public sealed class ResxLocalizationOptions
    {
        public string BaseName { get; init; } = "KIOSK.Resources.Resx.StringResources";
        public string DefaultCultureName { get; init; } = "en-US";
        public IReadOnlyList<CultureInfo> SupportedCultures { get; init; } =
            new[]
            {
                new CultureInfo("ko-KR"),
                new CultureInfo("en-US"),
                new CultureInfo("ja-JP"),
                new CultureInfo("zh-CN"),
                new CultureInfo("zh-TW"),
            };
    }
}
