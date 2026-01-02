using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIOSK.Domain.Entities;

namespace KIOSK.Infrastructure.Cache
{
    public class DatabaseCache
    {
        // ▼ UI 바인딩 용 (정렬된 리스트)
        public IReadOnlyList<KioskModel> Kiosk { get; set; } = Array.Empty<KioskModel>();
        public IReadOnlyList<DeviceModel> DeviceList { get; set; } = Array.Empty<DeviceModel>();
        public IReadOnlyList<LocaleInfoModel> LocaleInfoList { get; set; } = Array.Empty<LocaleInfoModel>();
        public IReadOnlyList<ReceiptModel> ReceiptList { get; set; } = Array.Empty<ReceiptModel>();
        public IReadOnlyList<ApiConfigModel> ApiConfigList { get; set; } = Array.Empty<ApiConfigModel>();
        public IReadOnlyList<DepositCurrencyModel> DepositCurrencyList { get; set; } = Array.Empty<DepositCurrencyModel>();
        public IReadOnlyList<WithdrawalCassetteModel> WithdrawalCassetteList { get; set; } = Array.Empty<WithdrawalCassetteModel>();
    }
}
