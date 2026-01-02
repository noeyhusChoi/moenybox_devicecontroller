using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.API.Cems
{
    public sealed class CemsApiResponse
    {
        public bool Result { get; init; }
        public string? ECode { get; init; }
        public Dictionary<string, string?> Fields { get; } = new();

        public static CemsApiResponse Parse(string raw)
        {
            // 서버가 JSON이면 JSON 파싱, key=value 라인형이면 Split
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;

                bool result = root.TryGetProperty("result", out var r) && r.GetBoolean();
                string? ecode = root.TryGetProperty("ecode", out var e) ? e.GetString() : null;

                var res = new CemsApiResponse { Result = result, ECode = ecode };

                foreach (var p in root.EnumerateObject())
                {
                    // 공통으로 빼둔 것 외에는 Fields에 전부 저장
                    if (p.Name is "result" or "ecode")
                        continue;

                    res.Fields[p.Name] = p.Value.ToString();
                }

                return res;
            }
            catch
            {
                // 서버가 true/false만 문자열로 줄 수도 있으니 최소 파싱
                var trimmed = raw.Trim().ToLowerInvariant();
                if (trimmed == "true" || trimmed == "false")
                    return new CemsApiResponse { Result = trimmed == "true" };
                return new CemsApiResponse { Result = false, ECode = "PARSE_ERROR" };
            }
        }

    }

    public static class CemsApiErrorCodeCatalog
    {
        private static readonly Dictionary<string, string> _map = new()
        {
            // 문서 '참고.오류코드' 반영
            // {"E001","인증 실패"}, {"E002","파라미터 누락"}, ...
        };

        public static string Humanize(string? ecode)
            => (ecode != null && _map.TryGetValue(ecode, out var msg)) ? msg : "알 수 없는 오류";
    }
}
