using System.Text.Json;

namespace KIOSK.Infrastructure.API.Gtf
{
    /// <summary>
    /// GTF API 공통 응답 래퍼
    /// </summary>
    public sealed class GtfApiResponse
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static T Parse<T>(string raw)
        {
            var obj = JsonSerializer.Deserialize<T>(raw, _options);
            if (obj == null)
                throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
            return obj;
        }
    }
}
