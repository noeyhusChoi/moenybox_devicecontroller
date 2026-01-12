using KIOSK.Domain.Entities;

namespace KIOSK.Application.Services.Localization
{
    public interface ILocaleInfoProvider
    {
        IReadOnlyList<LocaleInfoModel> LocaleInfoList { get; }
    }
}
