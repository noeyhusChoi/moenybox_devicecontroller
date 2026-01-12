using KIOSK.Domain.Entities;
using KIOSK.Application.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KIOSK.Application.Services
{
    public interface ITransactionServiceV2 : INotifyPropertyChanged
    {
        TransactionModelV2 Current { get; }

        // 거래 라이프사이클
        Task NewAsync(string sourceCcy, string targetCcy, CancellationToken ct = default);
        Task UpsertCustomerAsync(string idType, string name, string number, string nationality, CancellationToken ct = default);
        Task AddOrIncrementAsync(string currency, decimal denom, int deltaCount, CancellationToken ct = default);
        Task SetTargetRequestedAsync(decimal? targetAmount, CancellationToken ct = default);
        Task RefreshRatesAndPolicyAsync(CancellationToken ct = default);

        // 환율/정책 업서트(서비스 내부 저장)
        Task UpsertRateAsync(CurrencyPair pair, CancellationToken ct = default);
        Task UpsertPolicyAsync(string sourceCcy, string targetCcy, ExchangePolicy policy, CancellationToken ct = default);

        // ===== 출금 계획/실행 =====
        Task PlanPayoutsAsync(List<WithdrawalCassette> stock, CancellationToken ct = default);
        
        // === 출금 카세트 및 패킷 빌드 ===
        IReadOnlyList<(string DeviceID, byte[] Payload)> BuildDevicePackets(bool use20K = true);
        byte[] BuildPayload(IEnumerable<PayoutLine> cassettes);
        IReadOnlyList<(string DeviceID, byte[] Payload)> BuildDevicePackets(IEnumerable<PayoutLine> all);
        byte[] BuildPayload20K(IEnumerable<PayoutLine> cassettes);
        IReadOnlyList<(string DeviceID, byte[] Payload)> BuildDevicePackets20K(IEnumerable<PayoutLine> all);

        // === 출금 결과 업데이트 ===
        void ApplyDeviceResults(string deviceId, Dictionary<int, (int req, int exit, int rej)> resultMap);
        void ApplyDispenseResult(ObservableCollection<PayoutLine> payouts, string deviceId, Dictionary<int, (int req, int exit, int rej)> resultMap);
    }



    /// <summary>
    /// ※ 중요: 이 서비스의 모든 public 메서드는 UI 스레드에서 호출해야 합니다.
    /// (ObservableCollection/ObservableObject를 직접 수정/열거함)
    /// </summary>
    public sealed class TransactionServiceV2 : ITransactionServiceV2
    {
        // 내장 환율/정책 저장소
        private readonly ConcurrentDictionary<string, CurrencyPair> _rateTable = new();
        private readonly ConcurrentDictionary<(string Source, string Target), ExchangePolicy> _policyTable = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        public TransactionModelV2 Current { get; private set; } = new();

        public TransactionServiceV2()
        {
            Current = new TransactionModelV2();
            Current.PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        }

        public Task Reset()
        {
            Current = new TransactionModelV2();
            return Task.CompletedTask;
        }

        // ===== 거래 라이프사이클 =====
        public async Task NewAsync(string sourceCcy, string targetCcy, CancellationToken ct = default)
        {
            Current.TransactionDate = DateTime.Now;
            Current.TransactionID = DateTime.Now.ToString("yyyyMMddHHmmss");
            Current.TransactionType = sourceCcy == "KRW" ? "B" : "S";

            Current.Customer = new();
            Current.SourceCurrency = sourceCcy;
            Current.TargetCurrency = targetCcy;

            Current.Deposits.Clear();
            Current.TargetPayouts.Clear();
            Current.ChangePayouts.Clear();

            Current.TargetRequestedAmount = null;
            Current.SourceDepositedTotal = 0;
            Current.TargetComputedAmount = 0;
            Current.SourceRequiredAmount = 0;
            Current.SourceChangeAmount = 0;

            await RefreshRatesAndPolicyAsync(ct);
        }

        public Task UpsertCustomerAsync(string idType, string name, string number, string nationality, CancellationToken ct = default)
        {
            Current.Customer = new CustomerInfo
            {
                IdType = idType,
                CustomerName = name,
                CustomerNumber = number,
                CustomerNationality = nationality
            };
            return Task.CompletedTask;
        }

        public Task AddOrIncrementAsync(string currency, decimal denom, int deltaCount, CancellationToken ct = default)
        {
            Current.AddOrIncrement(currency, denom, deltaCount);
            return Task.CompletedTask;
        }

        public Task SetTargetRequestedAsync(decimal? targetAmount, CancellationToken ct = default)
        {
            Current.TargetRequestedAmount = targetAmount; // setter가 자동 Recalculate
            return Task.CompletedTask;
        }

        public Task RefreshRatesAndPolicyAsync(CancellationToken ct = default)
        {
            // 환율: (Source, Target) → 없으면 (Target, Source) 순으로 조회
            var s = Current.SourceCurrency.ToUpperInvariant();
            var t = Current.TargetCurrency.ToUpperInvariant();

            if (_rateTable.TryGetValue(s, out var pairST))
                Current.CurrencyPair = pairST; // setter → Recalculate

            // 정책
            if (_policyTable.TryGetValue((s, t), out var pol))
                Current.Policy = pol;

            // 보호적 재계산
            Current.Recalculate();
            return Task.CompletedTask;
        }

        // ===== 환율/정책 업서트 =====
        public Task UpsertRateAsync(CurrencyPair pair, CancellationToken ct = default)
        {
            if (pair is null) throw new ArgumentNullException(nameof(pair));
            if (pair.Rate <= 0m) throw new ArgumentOutOfRangeException(nameof(pair.Rate), "Rate must be > 0");

            var b = pair.BaseCurrency.ToUpperInvariant();

            _rateTable[b] = pair;

            return Task.CompletedTask;
        }

        public Task UpsertPolicyAsync(string sourceCcy, string targetCcy, ExchangePolicy policy, CancellationToken ct = default)
        {
            _policyTable[(sourceCcy.ToUpperInvariant(), targetCcy.ToUpperInvariant())] = policy;
            return Task.CompletedTask;
        }

        // ===== 출금 계획/실행 =====
        public Task PlanPayoutsAsync(List<WithdrawalCassette> stock, CancellationToken ct = default)
        {
            // UI 스레드에서 모델 컬렉션 수정
            Current.PlanPayouts(Current.TargetCurrency, Current.TargetComputedAmount, Current.TargetPayouts, stock, true, null);
            Current.PlanPayouts(Current.SourceCurrency, Current.SourceChangeAmount, Current.ChangePayouts, stock, true, null);

            return Task.CompletedTask;
        }


        // === 출금 카세트 및 패킷 빌드 ===   해당 섹션 다른 곳으로 이동 필요
        public IReadOnlyList<(string DeviceID, byte[] Payload)> BuildDevicePackets(bool use20K = true)
        {
            // UI 스레드에서 컬렉션 스냅샷
            var all = Current.TargetPayouts.Concat(Current.ChangePayouts).ToList();
            return use20K ? BuildDevicePackets20K(all) : BuildDevicePackets(all);
        }

        /// <summary>
        /// 동일 DispenserID를 가진 카세트 리스트로부터 14바이트 명령 페이로드 생성
        /// 슬롯 1~6, 슬롯당 최대 150장 제한
        /// </summary>
        public byte[] BuildPayload(IEnumerable<PayoutLine> cassettes)
        {
            if (cassettes == null)
                throw new ArgumentNullException(nameof(cassettes));

            var data = new byte[14];
            foreach (var c in cassettes)
            {
                if (c.Slot < 1 || c.Slot > 6)
                    continue;

                int cnt = Math.Clamp(c.RequestCount, 0, 150);
                data[c.Slot - 1] = (byte)cnt;
            }
            return data;
        }

        /// <summary>
        /// 여러 DispenserID가 포함된 카세트 집합에서
        /// 디바이스별 패킷을 그룹화하여 반환
        /// </summary>
        public IReadOnlyList<(string DeviceID, byte[] Payload)> BuildDevicePackets(IEnumerable<PayoutLine> all)
        {
            if (all == null)
                throw new ArgumentNullException(nameof(all));

            return all
                .GroupBy(c => c.DeviceID, StringComparer.OrdinalIgnoreCase)
                .Select(g => (DeviceID: g.Key, Payload: BuildPayload(g)))
                .ToList();
        }

        /// <summary>
        /// 20K 규격 Dispense DATA 빌드
        /// DATA = N + (Slot + Count3) * N  (ASCII)
        /// - Slot: '1'..'6'
        /// - Count3: 0~150, 3자리 0패딩 ("000"~"150")
        /// - N: 1~6 (이번 페이로드에 실제로 포함되는 슬롯 개수)
        /// </summary>
        public byte[] BuildPayload20K(IEnumerable<PayoutLine> cassettes)
        {
            if (cassettes == null) throw new ArgumentNullException(nameof(cassettes));

            // 1) 유효 슬롯만 추려서 0~150로 클램프, 0은 제외
            var entries = cassettes
                .Where(c => c != null && c.Slot >= 1 && c.Slot <= 6)
                .Select(c => new { Slot = c.Slot, Count = Math.Clamp(c.RequestCount, 0, 150) })
                .Where(x => x.Count > 0)
                .GroupBy(x => x.Slot)                       // 동일 슬롯 중복 들어오면 합산 (원하면 마지막 우선으로 바꿔도 됨)
                .Select(g => new { Slot = g.Key, Count = Math.Clamp(g.Sum(v => v.Count), 0, 150) })
                .OrderBy(x => x.Slot)
                .ToList();

            if (entries.Count == 0)
                throw new ArgumentException("요청된 방출 장수가 없습니다.", nameof(cassettes));

            if (entries.Count > 6)
                throw new ArgumentOutOfRangeException(nameof(cassettes), "카세트 개수는 최대 6개까지 포함할 수 있습니다.");

            // 2) 문자열로 직렬화 (ASCII)
            // 예: N=2, (1,3), (3,15) => "2" + "1" + "003" + "3" + "015"
            var sb = new StringBuilder(1 + entries.Count * 4);
            sb.Append((char)('0' + entries.Count));
            foreach (var e in entries)
            {
                sb.Append((char)('0' + e.Slot - 1));      // 슬롯 번호 1자리
                sb.Append(e.Count.ToString("000"));   // 3자리 0패딩
            }

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        /// <summary>
        /// 여러 DispenserID가 섞인 카세트 집합을 20K 규격 DATA로
        /// 디바이스별 그룹화하여 반환.
        /// </summary>
        public IReadOnlyList<(string DeviceID, byte[] Payload)> BuildDevicePackets20K(IEnumerable<PayoutLine> all)
        {
            if (all == null) throw new ArgumentNullException(nameof(all));

            return all
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.DeviceID))
                .GroupBy(c => c.DeviceID, StringComparer.OrdinalIgnoreCase)
                .Select(g => (DeviceID: g.Key, Payload: BuildPayload20K(g)))
                .ToList();
        }

        // === 출금 결과 업데이트 ===
        public void ApplyDeviceResults(string deviceId, Dictionary<int, (int req, int exit, int rej)> resultMap)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) return;
            if (resultMap is null || resultMap.Count == 0) return;

            // 타겟/거스름돈 모두에 적용
            ApplyDispenseResult(Current.TargetPayouts, deviceId, resultMap);
            ApplyDispenseResult(Current.ChangePayouts, deviceId, resultMap);
        }

        public void ApplyDispenseResult(ObservableCollection<PayoutLine> payouts, string deviceId, Dictionary<int, (int req, int exit, int rej)> resultMap)
        {
            if (payouts == null || resultMap == null) return;

            foreach (var p in payouts)
            {
                if (!p.DeviceID.Equals(deviceId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (resultMap.TryGetValue(p.Slot - 1, out var r))
                {
                    p.SucceededCount = r.exit;
                    p.FailedCount = p.RequestCount - r.exit;
                    p.RejectCount = r.rej;
                }
                else
                {
                    // 응답에 슬롯이 없는 경우
                    p.SucceededCount = 0;
                    p.FailedCount = p.RequestCount;
                    p.RejectCount = 0;
                }

                Trace.WriteLine($"[DispenseResult] Device={deviceId}, Slot={p.Slot}, Req={p.RequestCount}, Exit={p.SucceededCount}, Rej={p.RejectCount}");
            }

            Current.RecalculateDispenseOutcomeTotals();
        }
    }

}
