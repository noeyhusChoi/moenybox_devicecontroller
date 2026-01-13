using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Cache;
using KIOSK.Infrastructure.Management.Devices;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Text;

namespace KIOSK.Application.Services
{
    public partial class ReceiptPrintService
    {
        private readonly RecieptFormater _formater = new RecieptFormater();
        private readonly IDeviceManager _deviceManager;
        private readonly IMemoryCache _cache;

        public ReceiptPrintService(IDeviceManager deviceManager, IMemoryCache cache)
        {
            _deviceManager = deviceManager;
            _cache = cache;
        }

        private string? GetValue(string locale, string key)
        {
            var list = _cache.Get<IReadOnlyList<ReceiptModel>>(DatabaseCacheKeys.ReceiptList)
                ?? Array.Empty<ReceiptModel>();
            return list.FirstOrDefault(x => x.Locale == locale && x.Key == key)?.Value;
        }

        public async Task PrintReceiptAsync(string locale, TransactionModelV2 result)
        {
            // title
            var title = GetValue(locale, "title") + "\r\n";
            title += string.Concat(Enumerable.Repeat("\r\n", 2));
            await _deviceManager.SendAsync("PRINTER1", new DeviceCommand("PrintTitle", title));

            // company info
            string company = string.Empty;
            company += _formater.MakePadLeftString1(GetValue(locale, "company"));
            company += _formater.MakePadLeftString1(GetValue(locale, "tel"));
            company += _formater.MakePadLeftString1(GetValue(locale, "address"));
            company += string.Concat(Enumerable.Repeat("\r\n", 2));

            await _deviceManager.SendAsync("PRINTER1", new DeviceCommand("PrintContent", company));

            // date info
            string dateInfo = string.Empty;
            dateInfo += _formater.MakePadLeftString2(GetValue(locale, "transaction_time"), result.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss"));
            dateInfo += _formater.MakePadLeftString2(GetValue(locale, "transaction_number"), "A123456");
            await _deviceManager.SendAsync("PRINTER1", new DeviceCommand("PrintContent", dateInfo));

            // transaction info
            string transaction = string.Empty;
            transaction += transaction += _formater.MakePadLeftString1(string.Concat(Enumerable.Repeat("=", 48)));
            transaction += _formater.MakePadLeftString2(GetValue(locale, "transaction_currency"), result.CurrencyPair.BaseCurrency);
            transaction += _formater.MakePadLeftString2(GetValue(locale, "transaction_exchangerate"), result.CurrencyPair.Rate.ToString());
            transaction += _formater.MakePadLeftString2(GetValue(locale, "transaction_deposit"), $"{result.SourceDepositedTotal.ToString()} {result.SourceCurrency}");
            transaction += _formater.MakePadLeftString2(GetValue(locale, "transaction_withdrawal"), $"{result.TargetComputedAmount.ToString()} {result.TargetCurrency}");
            transaction += _formater.MakePadLeftString1(string.Concat(Enumerable.Repeat("=", 48)));
            transaction += string.Concat(Enumerable.Repeat("\r\n", 1));
            await _deviceManager.SendAsync("PRINTER1", new DeviceCommand("PrintContent", transaction));

            // kiosk info
            string shop = string.Empty;
            shop += _formater.MakePadLeftString3(GetValue(locale, "kiosk_name"));
            shop += _formater.MakePadLeftString3(GetValue(locale, "kiosk_address"));
            shop += _formater.MakePadLeftString3(GetValue(locale, "kiosk_tel"));
            shop += string.Concat(Enumerable.Repeat("\r\n", 2));
            await _deviceManager.SendAsync("PRINTER1", new DeviceCommand("PrintContent", shop));

            await _deviceManager.SendAsync("PRINTER1", new DeviceCommand("Cut"));
        }
        //shop += new string('=', 48);
    }

    public sealed class RecieptFormater
    {
        public string MakePadLeftString1(string? szText)
        {
            int nText = Encoding.GetEncoding("EUC-KR").GetByteCount(szText ?? " ");

            return string.Format("{0}\r\n", szText);
        }

        public string MakePadLeftString2(string? szText1, string? szText2)
        {
            int nText1 = Encoding.GetEncoding("EUC-KR").GetByteCount(szText1 ?? " ");
            int nText2 = Encoding.GetEncoding("EUC-KR").GetByteCount(szText2 ?? " ");
            int nSpace = 48 - (nText1 + nText2);

            string szSpace = new string(' ', nSpace);

            return string.Format("{0}{1}{2}\r\n", szText1, szSpace, szText2);
        }

        public string MakePadLeftString3(string? szText)
        {
            if (string.IsNullOrWhiteSpace(szText))
                return "\r\n";

            const int maxWidth = 48; // 최대 바이트 폭
            Encoding enc = Encoding.Default; // 프린터 인코딩 (CP949, EUC-KR 등)

            var words = szText.Split(' '); // 공백 단위로 분리
            var sb = new StringBuilder();
            var line = new StringBuilder();

            foreach (var word in words)
            {
                // 현재 라인 + 다음 단어(공백 포함)의 예상 바이트 수
                string candidate = (line.Length == 0 ? word : line + " " + word);
                int bytes = Encoding.GetEncoding("EUC-KR").GetByteCount(candidate);

                if (bytes <= maxWidth)
                {
                    // 아직 48바이트 안 넘음 → 그냥 붙이기
                    if (line.Length > 0)
                        line.Append(' ');
                    line.Append(word);
                }
                else
                {
                    // 넘었을 경우 → 이전 라인 출력 + 새 라인 시작
                    sb.AppendFormat("{0}{1}\r\n", new string(' ', maxWidth - Encoding.GetEncoding("EUC-KR").GetByteCount(line.ToString())), line);
                    line.Clear();
                    line.Append(word);
                }
            }

            // 마지막 줄 출력
            if (line.Length > 0)
                sb.AppendFormat("{0}{1}\r\n", new string(' ', maxWidth - Encoding.GetEncoding("EUC-KR").GetByteCount(line.ToString())), line);

            return sb.ToString();
        }




    }
}
