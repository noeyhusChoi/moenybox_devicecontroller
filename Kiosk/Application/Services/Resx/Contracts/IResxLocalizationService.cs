using System;
namespace Kiosk.Application.Services.Resx;

public interface IResxLocalizationService
{
    string? GetString(string key);

    event EventHandler? LanguageChanged;
}
