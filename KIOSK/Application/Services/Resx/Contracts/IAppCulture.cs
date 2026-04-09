using System;
using System.Collections.Generic;
using System.Globalization;

namespace Kiosk.Application.Services.Resx;

public interface IAppCulture
{
    CultureInfo CurrentCulture { get; }
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    void SetCulture(CultureInfo culture);

    event EventHandler? CultureChanged;
}
