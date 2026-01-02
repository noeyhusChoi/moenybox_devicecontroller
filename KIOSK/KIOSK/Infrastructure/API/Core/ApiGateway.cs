using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.API.Core
{
    public interface IApiGateway
    {
        Task<ApiSendResult> SendAsync(ApiEnvelope env, CancellationToken ct);
    }

    /// <summary>
    /// HttpClient 하나로 CEMS + REST 다 처리하는 공통 게이트웨이
    /// </summary>
    public sealed class ApiGateway : IApiGateway
    {
        private readonly HttpClient _http;

        public ApiGateway(HttpClient http)
        {
            _http = http;
        }

        public async Task<ApiSendResult> SendAsync(ApiEnvelope env, CancellationToken ct)
        {
            // 1) RouteTemplate에 RouteValues 치환
            var path = BuildPath(env.RouteTemplate, env.RouteValues);

            // 2) QueryString 붙이기
            if (env.Query is { Count: > 0 })
            {
                var qs = string.Join("&", env.Query.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
                path = $"{path}?{qs}";
            }

            using var req = new HttpRequestMessage(env.Method, path);

            // 3) 헤더
            if (env.Headers is { Count: > 0 })
            {
                foreach (var h in env.Headers)
                    req.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            req.Headers.TryAddWithoutValidation("Idempotency-Key", env.IdempotencyKey);

            // 4) Body(JSON) – REST API용, CEMS는 보통 null
            if (env.Body is not null &&
                (env.Method == HttpMethod.Post || env.Method == HttpMethod.Put || env.Method.Method == "PATCH"))
            {
                req.Content = JsonContent.Create(env.Body);
            }

            var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            return new ApiSendResult(resp.IsSuccessStatusCode, (int)resp.StatusCode,
                                     raw, resp.IsSuccessStatusCode ? null : raw);
        }

        private static string BuildPath(string template, IReadOnlyDictionary<string, string>? routeValues)
        {
            if (routeValues is null || routeValues.Count == 0) return template;

            var path = template;
            foreach (var kv in routeValues)
            {
                path = path.Replace($"{{{kv.Key}}}", Uri.EscapeDataString(kv.Value));
            }
            return path;
        }
    }
}
