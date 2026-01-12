using KIOSK.Infrastructure.API.Core;
using KIOSK.Domain.Entities;
using KIOSK.Application.Services;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace KIOSK.Infrastructure.API.Cems
{
    public interface ICemsApiCmdBuilder
    {
        ApiEnvelope C010(string currency);            // 환율조회(특정)
        ApiEnvelope C011();                           // 환율조회(전체)
        ApiEnvelope C020(string number);              // 환전한도조회
        ApiEnvelope C030(TransactionModelV2 s);       // 거래내역(거래등록)
        ApiEnvelope C040(IReadOnlySet<WithdrawalCassette> cash);    // 시재금 설정
        ApiEnvelope C060(DateTime dtKst, string msg); // 오류전달
        ApiEnvelope C070();                           // 시재 가져오기
        ApiEnvelope C090(DateTime time, string type, string msg);                           // SMS
    }

    public sealed class CemsApiCmdBuilder : ICemsApiCmdBuilder
    {
        private readonly CemsApiOptions _opt;

        public CemsApiCmdBuilder(IOptions<CemsApiOptions> opt)
        {
            _opt = opt.Value;
        }

        private static string Idem(CemsApiCmd cmd, string key)
            => $"{cmd}:{key}";

        private ApiEnvelope Env(CemsApiCmd cmd, Dictionary<string, string> qs, string idemKey = null)
        {
            var url = $"{_opt.BaseUrl}/api/cmdV2.php";

            return new ApiEnvelope(
              Provider: "CEMS",
              Method: HttpMethod.Get,
              RouteTemplate: url,
              RouteValues: null,
              Query: qs,
              Body: null,
              Headers: new Dictionary<string, string>(),
              IdempotencyKey: idemKey       // 옵션 사항
          );
        }


        public ApiEnvelope C010(string currency)
          => Env(CemsApiCmd.C010, new()
          {
              ["cmd"] = "C010",
              ["key"] = _opt.ApiKey,
              ["currency"] = currency
          });

        public ApiEnvelope C011()
            => Env(CemsApiCmd.C011, new()
            {
                ["cmd"] = "C011",
                ["key"] = _opt.ApiKey
            });
        public ApiEnvelope C020(string number)
          => Env(CemsApiCmd.C020, new()
          {
              ["cmd"] = "C020",
              ["key"] = _opt.ApiKey,
              ["number"] = number
          });

        public ApiEnvelope C030(TransactionModelV2 s)
        {
            // 기본 파라미터 세팅
            var dict = new Dictionary<string, string>
            {
                ["cmd"] = "C030",
                ["key"] = _opt.ApiKey,
            };

            #region 기본정보
            dict["dt"] = s.TransactionDate.ToString("yyyyMMddHHmm");    // 거래 일시
            dict["gubun"] = s.TransactionType.ToString();               // 거래 구분 (1:외화구매, 2:외화판매)
            dict["currency_code"] = s.CurrencyPair.BaseCurrency;        // 거래 화폐 코드
            dict["unique_key"] = "20251028104558";                      // 거래 고유키 (임시 하드코딩)
            dict["KIOSK_PID"] = "1";                                    // KIOSK 번호        
            dict["rate"] = s.CurrencyPair.Rate.ToString();              // 환율
            dict["input_money"] = s.SourceDepositedTotal.ToString();    // 입금 총액
            dict["output_money"] = s.TargetComputedAmount.ToString();   // 출금 총액
            dict["give_change"] = s.SourceChangeAmount.ToString();      // 원화 잔돈 (외화 구매 시)
            #endregion

            #region 고객정보
            dict["identity"] = s.Customer.IdType;
            dict["number"] = s.Customer.CustomerNumber;
            dict["name"] = s.Customer.CustomerName;
            dict["nation"] = s.Customer.CustomerNationality;
            #endregion

            #region 외화출금정보
            var payouts = s.TargetPayouts
                .Where(x => x.CurrencyCode != "KRW" && x.SucceededCount > 0)
                .OrderByDescending(x => x.Denomination)
                .Take(12)
                .ToList();

            for (int i = 0; i < 12; i++)
            {
                var keyC = $"c{i + 1}";
                var keyQ = $"qty{i + 1}";

                if (i < payouts.Count)
                {
                    var p = payouts[i];
                    dict[keyC] = p.Denomination.ToString();
                    dict[keyQ] = p.SucceededCount.ToString();
                }
                else
                {
                    dict[keyC] = "0";
                    dict[keyQ] = "0";
                }
            }
            #endregion

            #region 원화출금정보
            dict["krw1"] = SumBySuccessCount(s.TargetPayouts.Concat(s.ChangePayouts), "KRW", 50_000m);
            dict["krw2"] = SumBySuccessCount(s.TargetPayouts.Concat(s.ChangePayouts), "KRW", 10_000m);
            dict["krw3"] = SumBySuccessCount(s.TargetPayouts.Concat(s.ChangePayouts), "KRW", 5_000m);
            dict["krw4"] = SumBySuccessCount(s.TargetPayouts.Concat(s.ChangePayouts), "KRW", 1_000m);
            dict["krw5"] = SumBySuccessCount(s.TargetPayouts.Concat(s.ChangePayouts), "KRW", 500m);
            dict["krw6"] = SumBySuccessCount(s.TargetPayouts.Concat(s.ChangePayouts), "KRW", 100m);
            dict["krw7"] = SumBySuccessCount(s.TargetPayouts.Concat(s.ChangePayouts), "KRW", 50m);
            dict["krw8"] = SumBySuccessCount(s.TargetPayouts.Concat(s.ChangePayouts), "KRW", 10m);
            #endregion

            #region 출금오류정보
            dict["withdrawal_error"] = false.ToString();
            dict["error_amount"] = s.TargetFailedTotalAmount.ToString();
            dict["error_change_amount"] = s.ChangeFailedTotalAmount.ToString();
            dict["reject_krw_1"] = s.TargetPayouts.Concat(s.ChangePayouts)
                                          .Where(x => x.CurrencyCode == "KRW" && x.Denomination == 50_000m)
                                          .Sum(x => x.RejectCount).ToString();
            dict["reject_krw_2"] = s.TargetPayouts.Concat(s.ChangePayouts)
                                          .Where(x => x.CurrencyCode == "KRW" && x.Denomination == 10_000m)
                                          .Sum(x => x.RejectCount).ToString();
            dict["reject_krw_3"] = s.TargetPayouts.Concat(s.ChangePayouts)
                                          .Where(x => x.CurrencyCode == "KRW" && x.Denomination == 5_000m)
                                          .Sum(x => x.RejectCount).ToString();
            dict["reject_krw_4"] = s.TargetPayouts.Concat(s.ChangePayouts)
                                          .Where(x => x.CurrencyCode == "KRW" && x.Denomination == 1_000m)
                                          .Sum(x => x.RejectCount).ToString();
            dict["reject_for"] = s.TargetPayouts.Concat(s.ChangePayouts)
                                          .Where(x => x.CurrencyCode != "KRW")
                                          .Sum(x => x.RejectCount).ToString();
            #endregion

            // Env 래핑
            return Env(CemsApiCmd.C030, dict);
        }

        public ApiEnvelope C040(IReadOnlySet<WithdrawalCassette> cash)
        {
            // 1️. 기본 파라미터 세팅
            var dict = new Dictionary<string, string>
            {
                ["cmd"] = "C040",
                ["key"] = _opt.ApiKey,
            };

            #region 원화시재
            dict["krw_1"] = SumByCasstte(cash, "KRW", 50_000m);
            dict["krw_2"] = SumByCasstte(cash, "KRW", 10_000m);
            dict["krw_3"] = SumByCasstte(cash, "KRW", 5_000m);
            dict["krw_4"] = SumByCasstte(cash, "KRW", 1_000m);
            dict["krw_5"] = SumByCasstte(cash, "KRW", 500m);
            dict["krw_6"] = SumByCasstte(cash, "KRW", 100m);
            dict["krw_7"] = SumByCasstte(cash, "KRW", 50m);
            dict["krw_8"] = SumByCasstte(cash, "KRW", 10m);
            #endregion

            #region 외화시재
            dict["for_1"] = SumByCasstte(cash, "USD");
            dict["for_2"] = SumByCasstte(cash, "EUR");
            dict["for_3"] = SumByCasstte(cash, "CNY");
            dict["for_4"] = SumByCasstte(cash, "JPY");
            dict["for_5"] = SumByCasstte(cash, "HKD");
            dict["for_6"] = SumByCasstte(cash, "TWD");
            dict["for_7"] = SumByCasstte(cash, "PHP");
            dict["for_8"] = SumByCasstte(cash, "VND");
            #endregion

            return Env(CemsApiCmd.C040, dict);
        }

        public ApiEnvelope C060(DateTime dtKst, string msg)
            => Env(CemsApiCmd.C060, new()
            {
                ["cmd"] = "C060",
                ["key"] = _opt.ApiKey,
                ["dt"] = dtKst.ToString("yyyyMMddHHmmss"),
                ["error"] = msg
            });

        public ApiEnvelope C070()
            => Env(CemsApiCmd.C070, new()
            {
                ["cmd"] = "C070",
                ["key"] = _opt.ApiKey
            });

        public ApiEnvelope C090(DateTime time, string type, string msg)
            => Env(CemsApiCmd.C090, new()
            {
                ["cmd"] = "C090",
                ["key"] = _opt.ApiKey,
                ["dt"] = time.ToString("yyyyMMddHHmmss"),
                ["type"] = type,
                ["error"] = msg
            });

        private string SumByCasstte(IReadOnlySet<WithdrawalCassette>? source, string currencyCode, decimal? denomination = null)
        {
            if (source == null)
                return "0";

            var query = source
                .Where(x => string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));

            if (denomination.HasValue)
                query = query.Where(x => x.Denomination == denomination.Value);

            var sum = query.Sum(x => x.Count);
            return sum.ToString();
        }

        private string SumBySuccessCount(IEnumerable<PayoutLine>? source, string currencyCode, decimal? denomination = null)
        {
            if (source == null)
                return "0";

            var query = source
                .Where(x => string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));

            if (denomination.HasValue)
                query = query.Where(x => x.Denomination == denomination.Value);

            var sum = query.Sum(x => x.SucceededCount);
            return sum.ToString();
        }
    }
}
