using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.API.Core
{
    /// <summary>
    /// CEMS(쿼리스트링) + REST(JSON) 공용 HTTP Envelope
    /// </summary>
    public sealed record ApiEnvelope(
        string Provider,                                     // "CEMS", "REST1", "REST2" 등
        HttpMethod Method,                                   // GET, POST, PUT, DELETE ...
        string RouteTemplate,                                // "/api/cmdV2.php", "/v1/orders/{orderId}" 등
        IReadOnlyDictionary<string, string>? RouteValues,    // { "orderId": "123" } -> 템플릿 치환
        IReadOnlyDictionary<string, string>? Query,          // 쿼리스트링
        object? Body,                                        // JSON 바디 DTO (없으면 null)
        IReadOnlyDictionary<string, string>? Headers,        // 추가 헤더
        string IdempotencyKey                                // 멱등 키
    );

    public sealed record ApiSendResult(
    bool Success,
    int StatusCode,
    string Raw,
    string? Error);
}
