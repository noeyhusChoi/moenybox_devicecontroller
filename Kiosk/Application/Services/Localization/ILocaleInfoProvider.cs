using Kiosk.Infrastructure.Database.Models;

namespace Kiosk.Application.Services.Localization
{
    public interface ILocaleInfoProvider
    {
        IReadOnlyList<LocaleInfoModel> LocaleInfoList { get; }
    }
}
