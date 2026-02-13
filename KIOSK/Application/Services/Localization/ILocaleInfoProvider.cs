using KIOSK.Infrastructure.Database.Models;

namespace KIOSK.Application.Services.Localization
{
    public interface ILocaleInfoProvider
    {
        IReadOnlyList<LocaleInfoModel> LocaleInfoList { get; }
    }
}
