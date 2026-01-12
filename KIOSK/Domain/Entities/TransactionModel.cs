using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Application.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Domain.Entities
{
    public enum RoundingMode
    {
        Down,   // 버림
        Up,     // 올림
        Nearest // 반올림
    }

    // 통화쌍: Rate = Quote per 1 Base (예: USD/KRW = 1400)
    public record CurrencyPair(string BaseCurrency, decimal Rate);

    // 수수료/반올림/증분 규칙
    public partial class ExchangePolicy : ObservableObject
    {
        [ObservableProperty] private decimal feePercent;     // 예: 0.0m ~ 0.05m (5%)
        [ObservableProperty] private decimal feeFlat;        // 통화 단위로(적용 위치는 아래 로직 참고)
        [ObservableProperty] private decimal targetIncrement; // 타겟 지급 최소단위(예: KRW=100, USD=0.01)
        [ObservableProperty] private RoundingMode roundingMode = RoundingMode.Down;
    }

    // 고객 정보
    public partial class CustomerInfo : ObservableObject
    {
        [ObservableProperty] private string idType = "1";          // 1.여권, 2.주민등록증, 3.운전면허증, 4. 기타
        [ObservableProperty] private string customerName = "2";
        [ObservableProperty] private string customerNumber = "3";
        [ObservableProperty] private string customerNationality = "4";
    }

    // 입금 라인 (여러 통화 섞일 수 있지만 합계산은 SourceCurrency만 집계)
    public partial class DepositNoteV2 : ObservableObject
    {
        [ObservableProperty] private string currencyCode = ""; // ex) KRW
        [ObservableProperty] private decimal denomination;        // ex) 5000
        [ObservableProperty] private int count;            // 누적 장수
        [ObservableProperty] private decimal amount;       // denom * count

        partial void OnCountChanged(int value) => Recalculate();
        partial void OnDenominationChanged(decimal value) => Recalculate();

        private void Recalculate() => Amount = Denomination * Count;
    }

    // 공통 출금 라인 (타겟 또는 거스름돈 모두에 사용)
    public partial class PayoutLine : ObservableObject
    {
        [ObservableProperty] private string deviceID = ""; // ex) HCDM1
        [ObservableProperty] private string currencyCode = ""; // ex) KRW or USD
        [ObservableProperty] private int slot;             // ex) KRW or USD
        [ObservableProperty] private decimal denomination;        // ex) 10000
        [ObservableProperty] private int requestCount;     // 요청 장수
        [ObservableProperty] private int succeededCount;   // 성공 장수
        [ObservableProperty] private int failedCount;      // 실패 장수
        [ObservableProperty] private int rejectCount;      // 리젝 장수
    }

    public partial class TransactionModelV2 : ObservableObject
    {
        // === 거래 메타 ===
        [ObservableProperty] private DateTime transactionDate = DateTime.Now;
        [ObservableProperty] private string transactionID = "";
        [ObservableProperty] private string transactionType = "";

        // 고객 정보
        [ObservableProperty] private CustomerInfo customer = new();

        // 고객이 넣는 통화 / 기기가 내주는 통화
        [ObservableProperty] private string sourceCurrency = "KRW";
        [ObservableProperty] private string targetCurrency = "USD";

        // 환율: Rate = Quote per 1 Base (Base/Quote)
        [ObservableProperty] private CurrencyPair currencyPair = new("USD", 1400m);

        // 정책(수수료/증분/반올림)
        [ObservableProperty]
        private ExchangePolicy policy = new()
        {
            FeePercent = 0m,
            FeeFlat = 0m,
            TargetIncrement = 100m, // KRW 기준 예시; USD면 0.01 등으로 세팅
            RoundingMode = RoundingMode.Down
        };

        // 입금
        public ObservableCollection<DepositNoteV2> Deposits { get; } = new();

        // 타겟 금액 "요청" (Buy FX: 고객이 USD 100을 원함)
        // Sell FX(외화 -> 원화) 시엔 null로 두고, 입금합으로부터 타겟을 계산
        [ObservableProperty] private decimal? targetRequestedAmount;

        // === 계산 결과 레코드 ===
        [ObservableProperty] private decimal sourceDepositedTotal; // 입금 총액 (SourceCurrency 필터 합계)
        [ObservableProperty] private decimal targetComputedAmount; // 출금 총액 (실제 지급(증분/반올림 적용 후))
        [ObservableProperty] private decimal sourceRequiredAmount; // 타겟을 사기 위해 필요한 소스 금액(Buy FX 케이스)
        [ObservableProperty] private decimal sourceChangeAmount;   // 잔돈 (소스 통화)

        // 출금 계획: 타겟 / 거스름돈
        public ObservableCollection<PayoutLine> TargetPayouts { get; } = new();
        public ObservableCollection<PayoutLine> ChangePayouts { get; } = new();

        // 잔돈
        [ObservableProperty] private decimal targetMinorRemainderAmount;  // 최저 권종 미만 잔액(포인트 전환)
        [ObservableProperty] private decimal changeMinorRemainderAmount;  // 최저 권종 미만 잔액(포인트 전환)

        // 출금 결과
        // 출금 결과 합계
        [ObservableProperty] private decimal targetSucceededTotalAmount;   // succeed * denom 총합
        [ObservableProperty] private decimal targetFailedTotalAmount;   // fail * denom 총합
        [ObservableProperty] private decimal targetRejectedTotalAmount; // rej * denom 총합

        // 출금 결과 합계 (거스름돈)
        [ObservableProperty] private decimal changeSucceededTotalAmount;   // succeed * denom 총합
        [ObservableProperty] private decimal changeFailedTotalAmount;   // fail * denom 총합
        [ObservableProperty] private decimal changeRejectedTotalAmount; // rej * denom 총합

        public TransactionModelV2()
        {
            Deposits.CollectionChanged += (_, __) =>
            {
                HookDepositPropertyChanged();
                Recalculate();
            };
            HookDepositPropertyChanged();
        }

        private void HookDepositPropertyChanged()
        {
            foreach (var d in Deposits)
                d.PropertyChanged -= OnDepositPropertyChanged;
            foreach (var d in Deposits)
                d.PropertyChanged += OnDepositPropertyChanged;
        }

        private void OnDepositPropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DepositNoteV2.Amount) or nameof(DepositNoteV2.CurrencyCode))
                Recalculate();
        }

        // 주요 파라미터 바뀌면 즉시 재계산
        partial void OnSourceCurrencyChanged(string value) => Recalculate();
        partial void OnTargetCurrencyChanged(string value) => Recalculate();
        partial void OnCurrencyPairChanged(CurrencyPair value) => Recalculate();
        partial void OnPolicyChanged(ExchangePolicy value) => Recalculate();
        partial void OnTargetRequestedAmountChanged(decimal? value) => Recalculate();

        // 입금 추가 또는 장수 증가
        public void AddOrIncrement(string currency, decimal denom, int deltaCount)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException(nameof(currency));

            // 이미 존재하는 동일 통화/액면 라인을 찾음
            var existing = Deposits.FirstOrDefault(d =>
                d.CurrencyCode.Equals(currency, StringComparison.OrdinalIgnoreCase) &&
                d.Denomination == denom);

            if (existing != null)
            {
                // Count 증가 (ObservableObject라 UI 자동 갱신)
                existing.Count += deltaCount;
            }
            else
            {
                // 없으면 새로 추가
                Deposits.Add(new DepositNoteV2
                {
                    CurrencyCode = currency,
                    Denomination = denom,
                    Count = deltaCount
                });
            }
        }


        // currencyPair: Base/Quote, Rate = Quote per 1 Base
        // 소스→타겟 변환 시 필요한 환율을 얻음
        private decimal GetSourceToTargetRate()
        {
            return CurrencyPair.Rate;
        }

        private static decimal ApplyRounding(decimal amount, decimal increment, RoundingMode mode)
        {
            if (increment <= 0) return amount;

            decimal units = amount / increment;

            return mode switch
            {
                RoundingMode.Down => Math.Floor(units) * increment,
                RoundingMode.Up => Math.Ceiling(units) * increment,
                RoundingMode.Nearest => Math.Round(units, MidpointRounding.AwayFromZero) * increment,
                _ => amount
            };
        }

        // 수수료 적용: 기본적으로 "타겟 기준 차감형" 예시
        // - Sell FX(소스→타겟 환전): 타겟 금액에서 feePercent/feeFlat 차감 후 반올림
        // - Buy FX(타겟 고정): 요구 타겟을 달성하기 위해 소스 요구액을 수수료까지 "그로스업" 계산
        private (decimal netTarget, decimal totalFee) ApplyFeesForTarget(decimal grossTarget)
        {
            var percentFee = grossTarget * Policy.FeePercent;
            var totalFee = percentFee + Policy.FeeFlat;
            var net = grossTarget - totalFee;
            return (net, totalFee);
        }

        private (decimal requiredSource, decimal totalFee) GrossUpFeesOnTarget(decimal desiredTarget)
        {
            // desiredTarget = grossTarget - (grossTarget*fee% + feeFlat)
            // => desiredTarget = grossTarget*(1-fee%) - feeFlat
            // => grossTarget = (desiredTarget + feeFlat) / (1 - fee%)
            decimal denom = (1m - Policy.FeePercent);
            if (denom <= 0m) throw new InvalidOperationException("FeePercent too large.");
            var grossTarget = (desiredTarget + Policy.FeeFlat) / denom;

            // 소스 요구액 = grossTarget / (소스→타겟 환율)
            var rate = GetSourceToTargetRate();
            var requiredSource = grossTarget / rate;

            var totalFee = (grossTarget * Policy.FeePercent) + Policy.FeeFlat;
            return (requiredSource, totalFee);
        }

        // === 메인 계산 ===
        public void Recalculate()
        {
            // 1) 소스 통화 입금 합계
            SourceDepositedTotal = Deposits
                .Where(d => d.CurrencyCode.Equals(SourceCurrency, StringComparison.OrdinalIgnoreCase))
                .Sum(d => d.Amount);

            // 2) 환율 방향 확보
            var rate = GetSourceToTargetRate();

            // 3) 분기
            if (TargetRequestedAmount is null)
            {
                // --- Sell FX: 외화 판매 ---
                // (a) 타겟 "그로스" = Source * rate
                var grossTarget = SourceDepositedTotal * rate;

                // (b) 수수료 차감
                var (netTarget, _) = ApplyFeesForTarget(grossTarget);

                // (c) 지급단위 반올림
                TargetComputedAmount = ApplyRounding(netTarget, Policy.TargetIncrement, Policy.RoundingMode);

                // (d) 소스 요구액은 '입금액' 자체, 거스름돈 없음(보통 이 케이스는 소스 모두 수납)
                SourceRequiredAmount = 0m; // SourceDepositedTotal;
                SourceChangeAmount = 0m;

                // (e) 100단위는 포인트 전환
                TargetMinorRemainderAmount = TargetComputedAmount % 1000;
                //TargetComputedAmount = TargetComputedAmount - TargetMinorRemainderAmount;
            }
            else
            {
                // --- Buy FX: 외화 구매 ---
                var desiredTarget = TargetRequestedAmount.Value;

                // (a) 지급단위에 맞춰 타겟을 먼저 반올림(지급 가능 최소단위에 맞게)
                //var roundedDesiredTarget = ApplyRounding(desiredTarget, Policy.TargetIncrement, Policy.RoundingMode);
                var roundedDesiredTarget = desiredTarget;

                // (b) 수수료 포함으로 소스 요구액 산출(그로스업)
                var (requiredSourceRaw, _) = GrossUpFeesOnTarget(desiredTarget);

                // (c) 소스 요구액도 기계/현금단위 제약이 있으면 반올림 정책을 별도로 둘 수 있음
                //    여기선 그대로 사용(필요 시 별도 Policy.SourceIncrement 추가)
                //SourceRequiredAmount = Math.Round(requiredSourceRaw, 0, MidpointRounding.AwayFromZero);
                SourceRequiredAmount = ApplyRounding(requiredSourceRaw, Policy.TargetIncrement, Policy.RoundingMode);

                // (d) 입금이 충분한지 체크
                if (SourceDepositedTotal >= SourceRequiredAmount)
                {
                    // (e) 충분 → 타겟 그대로 지급, 남는 돈은 소스 통화 거스름돈
                    TargetComputedAmount = roundedDesiredTarget;
                    SourceChangeAmount = SourceDepositedTotal - SourceRequiredAmount;

                    // (e) 100단위는 포인트 전환
                    ChangeMinorRemainderAmount = SourceChangeAmount % 1000;
                    //SourceChangeAmount = SourceChangeAmount - ChangeMinorRemainderAmount;
                }
                else
                {
                    // 부족 → 가능한 만큼만 타겟을 재산출 (부분 체결 로직)
                    // grossTarget = SourceDepositedTotal * rate
                    // 수수료 차감 후 지급 가능한 타겟 계산
                    var grossTarget = SourceDepositedTotal * rate;
                    var (netTarget, _) = ApplyFeesForTarget(grossTarget);
                    TargetComputedAmount = ApplyRounding(netTarget, Policy.TargetIncrement, Policy.RoundingMode);

                    // 이 경우 거스름돈은 0 (전액 사용)
                    SourceChangeAmount = 0m;
                }
            }

            // 4) 출금 계획(지폐 조합)은 별도 알고리즘으로 구성
            //    - TargetPayouts  : TargetComputedAmount를 타겟 통화 카세트에서 분해
            //    - ChangePayouts  : SourceChangeAmount를 소스 통화 카세트에서 분해
            // 여기서는 기존 분해 로직/재고 기반 그리디 알고리즘을 연결할 수 있게 메서드 시그니처만 둠.
            // PlanPayouts(TargetCurrency, TargetComputedAmount, TargetPayouts, targetStock);
            // PlanPayouts(SourceCurrency, SourceChangeAmount, ChangePayouts, sourceStock);
        }


        // === 출금 계산 ===
        public void PlanPayouts(
            string payoutCurrency,
            decimal payoutAmount,
            ObservableCollection<PayoutLine> targetCollection,
            IEnumerable<WithdrawalCassette> cassettes,
            bool preferHigherDenom = true,
            int? maxNotesTotal = null,
            Func<WithdrawalCassette, int, int>? perCassetteLimiter = null) // (cassette, need) -> cap
        {
            targetCollection.Clear();
            if (payoutAmount <= 0) return;

            // 1) 통화/유효 재고 필터
            var list = cassettes
                .Where(c => c.CurrencyCode.Equals(payoutCurrency, StringComparison.OrdinalIgnoreCase)
                            && c.Denomination > 0m
                            && c.Count > 0)
                .ToList();

            if (list.Count == 0) return;

            // 2) 정렬: 권종(고액 우선 또는 소액 우선) → DeviceID → Slot
            list = preferHigherDenom
                ? list.OrderByDescending(c => c.Denomination).ThenBy(c => c.DeviceID).ThenBy(c => c.Slot).ToList()
                : list.OrderBy(c => c.Denomination).ThenBy(c => c.DeviceID).ThenBy(c => c.Slot).ToList();

            decimal remaining = payoutAmount;
            int notesUsed = 0;

            // 3) 카세트 순회하며 필요 장수 채우기
            foreach (var c in list)
            {
                if (remaining <= 0m) break;

                // 이 권종으로 필요한 장수
                var needCount = (int)(remaining / c.Denomination);
                if (needCount <= 0) continue;

                // 카세트 보유량/한도 적용
                int use = Math.Min(needCount, c.Count);

                if (maxNotesTotal is int capTotal)
                    use = Math.Min(use, Math.Max(0, capTotal - notesUsed));

                if (perCassetteLimiter is not null)
                    use = Math.Min(use, Math.Max(0, perCassetteLimiter(c, needCount)));

                if (use <= 0) continue;

                targetCollection.Add(new PayoutLine
                {
                    DeviceID = c.DeviceID,
                    CurrencyCode = payoutCurrency,
                    Slot = c.Slot,
                    Denomination = c.Denomination,
                    RequestCount = use,
                    SucceededCount = 0,
                    FailedCount = 0,
                    RejectCount = 0
                });

                notesUsed += use;
                remaining -= use * c.Denomination;
            }
        }

        public void RecalculateDispenseOutcomeTotals()
        {
            // 성공, 실패, 리젝트 각 총 금액
            TargetSucceededTotalAmount = TargetPayouts.Sum(p => Math.Max(0, p.SucceededCount) * p.Denomination);
            TargetFailedTotalAmount = TargetPayouts.Sum(p => Math.Max(0, p.RequestCount - p.SucceededCount) * p.Denomination);
            TargetRejectedTotalAmount = TargetPayouts.Sum(p => Math.Max(0, p.FailedCount) * p.Denomination);

            ChangeSucceededTotalAmount = ChangePayouts.Sum(p => Math.Max(0, p.SucceededCount) * p.Denomination);
            ChangeFailedTotalAmount = ChangePayouts.Sum(p => Math.Max(0, p.RequestCount - p.SucceededCount) * p.Denomination);
            ChangeRejectedTotalAmount = ChangePayouts.Sum(p => Math.Max(0, p.FailedCount) * p.Denomination);
        }
    }
}
