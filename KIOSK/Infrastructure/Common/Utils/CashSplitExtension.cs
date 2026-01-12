namespace KIOSK.Infrastructure.Common.Utils
{
    internal class CashSplitExtension
    {
        /// <summary>
        /// amount을 큰 단위 지폐부터 각 단위 지폐 보유 갯수를 고려하여 분배
        /// stock: {지폐단위: 보유장수}
        /// 반환: (할당 결과, 남는 금액)
        /// </summary>
        public (Dictionary<int, int> counts, int remainder) SplitNotesWithStock(
            int amount,
            IReadOnlyDictionary<int, int> stock)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (stock == null || stock.Count == 0)
                throw new ArgumentException("stock이 비어있습니다.");

            var result = new Dictionary<int, int>();
            int remaining = amount;

            // 🔹 stock의 key(지폐 단위)를 내림차순 정렬
            foreach (var d in stock.Keys.OrderByDescending(k => k))
            {
                int have = stock[d];
                if (d <= 0 || have <= 0)
                    continue;

                // 이 단위로 필요한 최대 장수
                int need = remaining / d;
                // 재고 한도 내에서 사용
                int use = Math.Min(need, have);

                result[d] = use;
                remaining -= use * d;

                if (remaining == 0)
                    break;
            }

            return (result, remaining);
        }
    }
}
